# API Production Hardening — Design Spec

**Date:** 2026-03-16
**Scope:** Organization, Members, Roles, Settings, Invites, Super Admin endpoints
**Goal:** Production-ready, consistent, permission-protected, professionally documented API

---

## 1. Design Decisions

### 1.1 Entity Lifecycle Rules

| Entity | Deactivate | Hard Delete | Who Can |
|--------|-----------|-------------|---------|
| **Organization** | `is_active = false` | CASCADE delete (all data gone) | Super admin only |
| **User** | `is_active = false` | Row deleted, FKs SET NULL (GDPR) | Super admin only |
| **Org membership** | `status = 'deactivated'` (temporary) | Row deleted from `organization_users` + CASCADE `organization_user_roles` (permanent) | Deactivate: org owner + super admin. Delete: super admin only |

**Rules:**
- Org deactivate → all members deactivated, no one can access
- Org reactivate → org active again, members stay deactivated (must be individually reactivated)
- User deactivate → user cannot log in to any org
- User delete → GDPR right-to-be-forgotten, all `created_by_id`/`updated_by_id` refs become NULL
- Member deactivate → temporary suspension, roles preserved, can be reactivated
- Member remove (hard delete) → permanent removal, roles deleted, requires new invite to rejoin
- **Self-deactivation prohibited**: A user cannot deactivate their own membership (error: `Member.CannotDeactivateSelf`). Super admins also cannot deactivate their own admin record.

### 1.2 Authentication Flow

```
Login → Clerk session
      → FE reads org list from Clerk
      → FE reads publicMetadata.systemRole from Clerk (admin check)
      → User selects org → GET /v1/me (org-scoped)
      → User selects admin dashboard → GET /v1/admin/me
```

- **`/v1/me`**: Org-scoped only. Returns roles, permissions for current org. **No `isSystemAdmin` field.**
- **`/v1/admin/me`**: Super admin profile. Returns admin role, assignment date, etc.
- **Clerk `publicMetadata`**: `{ "systemRole": "super_admin" }` — FE reads this for UI decisions
- **Clerk `privateMetadata`**: `{ "systemRole": "super_admin" }` — API validates this server-side

### 1.3 Invite Flow

| Scenario | Action |
|----------|--------|
| **User exists in Clerk** | Add to Clerk org + create `organization_users` (active) + create `organization_user_roles` + create `invites` record (accepted, audit trail). |
| **User not in Clerk** | Create `invites` record (pending) + send Clerk invitation email. On acceptance: webhook → create user → create org membership → mark invite accepted. |

**Invite rules:**
- `expires_at` default: `now() + 7 days` (DB-level default)
- Expired invites cannot be accepted
- Only pending invites can be canceled
- Canceled invites cannot be reused — new invite required
- One pending invite per email per org (DB unique constraint)

### 1.4 Role Management Rules

- 3 system roles: **Owner**, **Manager**, **Viewer** — cannot be edited or deleted
- Custom roles: org-scoped, can be created/edited/deleted by org owner
- **Last Owner protection**: Cannot remove Owner role from last owner
- **Custom role deletion**: All members with that role get **Viewer** role assigned (if not already Viewer), then role deleted
- Users can have multiple roles — permissions are the union of all role permissions

### 1.5 Error Handling

**Standard: RFC 9457 Problem Details** (ASP.NET Core built-in)

Two categories, single format:

```json
// Validation error (400)
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "email": ["Email is required."],
    "name": ["Name must be between 2 and 100 characters."]
  }
}

// Business error (404, 409, etc.)
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "Member.NotFound",
  "status": 404,
  "detail": "The specified member was not found in this organization."
}
```

**Error code format:** `Entity.ErrorName` (PascalCase dot-notation)

**HTTP status mapping:**
| ErrorType | Status | Usage |
|-----------|--------|-------|
| Validation | 400 | Field-level input errors (FluentValidation) |
| Failure | 400 | General bad request (business rule) |
| Unauthorized | 401 | Not authenticated |
| Forbidden | 403 | Authenticated but not permitted |
| NotFound | 404 | Resource does not exist |
| Conflict | 409 | State conflict (already exists, already deactivated) |
| ExternalService | 502 | Third-party failure (Clerk, Resend) |

### 1.6 American English

All code, error messages, log messages, comments must use American English.

**Current violations to fix (23+ instances):**
- `cancelled` → `canceled` (7 instances: Invite entity, EF config, error messages, exception handler, `CancelInviteCommand.CancelledByUserId`, `CancelInviteHandler` assignment + log message)
- `organisation` → `organization` (14 instances: log messages, error messages)
- Inline error `"Organisation is not linked to Clerk"` in invite handler → should use error constant + American English

**Critical:** `Invite.Statuses.Cancelled = "cancelled"` but DB CHECK constraint uses `"canceled"`. This is a **runtime bug** — EF Core writes "cancelled" but DB expects "canceled".

---

## 2. Endpoint Inventory

### 2.1 Current User — `/v1/me`

#### `GET /v1/me`
**Permission:** Authenticated + active org member
**Flow:**
1. Resolve user from JWT via `IUserContext`
2. Query user profile, org info, roles, permissions via Dapper
3. Return `MeResponse` (without `isSystemAdmin`)

**Response:** `200 OK`
```json
{
  "userId": "usr_xxx",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "organization": { "organizationId": "org_xxx", "name": "Acme", "slug": "acme" },
  "roles": ["Owner"],
  "permissions": ["org:settings:read", "org:members:manage", ...]
}
```

**Errors:** `404 Me.UserNotFound`

**Changes needed:**
- Remove `isSystemAdmin` from `MeResponse`
- Remove `IsSystemAdmin` from Dapper query

---

### 2.2 Organization Settings — `/v1/organizations/settings`

#### `GET /v1/organizations/settings`
**Permission:** `org:settings:read`
**Flow:**
1. Resolve org from `IUserContext.OrganizationId`
2. Query org settings via Dapper
3. Return settings (name, contact, address, legal, currency, timezone)

**Response:** `200 OK` — `OrganizationSettingsResponse`
**Errors:** `404 Organization.NotFound`

#### `PATCH /v1/organizations/settings`
**Permission:** `org:settings:update`
**Flow:**
1. Validate request (FluentValidation)
2. Load org via EF Core
3. Apply partial update (only non-null fields)
4. Save via UnitOfWork

**Response:** `204 No Content`
**Errors:** `400 Validation`, `404 Organization.NotFound`

**Changes needed:** None — these are correct.

---

### 2.3 Members — `/v1/organizations/members`

#### `GET /v1/organizations/members`
**Permission:** `org:members:read`
**Flow:**
1. Resolve org from `IUserContext`
2. Query members with roles via Dapper (paginated, searchable by name/email, filterable by status/role)
3. Return paginated list

**Response:** `200 OK` — `PagedResult<MemberListItemResponse>`

#### `GET /v1/organizations/members/{id}`
**Permission:** `org:members:read`
**Flow:**
1. Resolve member by public user ID within current org
2. Return member detail with roles

**Response:** `200 OK` — `MemberDetailResponse`
**Errors:** `404 Member.NotFound`

#### `PATCH /v1/organizations/members/{id}/roles`
**Permission:** `org:members:manage`
**Flow:**
1. Validate role IDs exist and are accessible to this org
2. Check last-owner protection (if removing Owner role, ensure other owners exist)
3. Replace all role assignments for this user in this org
4. Invalidate permission cache

**Response:** `204 No Content`
**Errors:** `400 Validation`, `404 Member.NotFound`, `409 Member.CannotRemoveLastOwner`, `400 Member.InvalidRoleIds`

**Changes needed:**
- Current URL is `PATCH /v1/organizations/members/{id}` — should be `PATCH .../members/{id}/roles` (add `/roles` suffix + standardize param to `{id}`)

#### `PATCH /v1/organizations/members/{id}/deactivate`
**Permission:** `org:members:manage`
**Flow:**
1. Verify member exists and is active
2. **Block self-deactivation** (cannot deactivate yourself)
3. Check last-owner protection
4. Set `organization_users.status = 'deactivated'`
5. Invalidate permission cache

**Response:** `204 No Content`
**Errors:** `404 Member.NotFound`, `403 Member.CannotDeactivateSelf`, `409 Member.AlreadyDeactivated`, `409 Member.CannotDeactivateLastOwner`

**Changes needed:**
- Current method is `POST` — should be `PATCH` (state transition per conventions)

#### `PATCH /v1/organizations/members/{id}/reactivate`
**Permission:** `org:members:manage`
**Flow:**
1. Verify member exists and is deactivated
2. Set `organization_users.status = 'active'`
3. Invalidate permission cache

**Response:** `204 No Content`
**Errors:** `404 Member.NotFound`, `409 Member.AlreadyActive`

**Changes needed:**
- Current method is `POST` — should be `PATCH`

---

### 2.4 Roles — `/v1/organizations/roles`

#### `GET /v1/organizations/roles`
**Permission:** `org:roles:read`
**Flow:**
1. Query all roles accessible to this org (system + custom) via Dapper
2. Include permission codes and member count per role

**Response:** `200 OK` — `PagedResult<RoleResponse>`

#### `GET /v1/organizations/roles/{id}`
**Permission:** `org:roles:read`
**Flow:**
1. Query role by public ID, verify it belongs to this org (or is system role)
2. Return role detail with permissions

**Response:** `200 OK` — `RoleResponse`
**Errors:** `404 Role.NotFound`

#### `POST /v1/organizations/roles`
**Permission:** `org:roles:manage`
**Flow:**
1. Validate name uniqueness within org
2. Validate permission codes exist
3. Create role + role_permissions
4. Return created role public ID

**Response:** `201 Created` with `Location` header
**Errors:** `400 Validation`, `409 Role.NameAlreadyExists`

#### `PATCH /v1/organizations/roles/{roleId}`
**Permission:** `org:roles:manage`
**Flow:**
1. Verify role exists and is not system role
2. Validate name uniqueness (if changed)
3. Update role fields + replace permissions
4. Invalidate permission cache for all members with this role

**Response:** `204 No Content`
**Errors:** `400 Validation`, `404 Role.NotFound`, `403 Role.IsSystem`, `409 Role.NameAlreadyExists`

#### `DELETE /v1/organizations/roles/{roleId}`
**Permission:** `org:roles:manage`
**Flow:**
1. Verify role exists, is not system role, belongs to this org
2. Find all members with this role
3. For each member: if they don't already have Viewer role, assign Viewer
4. Delete role (CASCADE deletes role_permissions and organization_user_roles for this role)
5. Invalidate permission cache

**Response:** `204 No Content`
**Errors:** `404 Role.NotFound`, `403 Role.IsSystem`

#### `GET /v1/organizations/permissions`
**Permission:** `org:roles:read`
**Flow:**
1. Query all permissions grouped by category via Dapper

**Response:** `200 OK` — `List<PermissionGroupResponse>`

---

### 2.5 Invites — `/v1/organizations/invites`

#### `POST /v1/organizations/invites`
**Permission:** `org:members:manage`
**Flow:**
1. Validate email format, role exists
2. Check if user already exists in Clerk (by email)
3. **If user exists in Clerk:**
   - Verify not already a member of this org
   - Add to Clerk org
   - Create `organization_users` (active) + `organization_user_roles`
   - Create `invites` record (accepted, audit trail — "who invited whom")
   - Return `200 OK` with member info
4. **If user not in Clerk:**
   - Verify no pending invite for this email+org
   - Create invite record (pending, expires_at = now + 7 days)
   - Send Clerk invitation email
   - Return `201 Created` with invite info

**Response:** `200 OK` (existing user added) or `201 Created` (invite sent)
**Errors:** `400 Validation`, `404 Invite.RoleNotFound`, `409 Invite.AlreadyMember`, `409 Invite.AlreadyPending`, `502 Invite.ClerkSyncFailed`

**Changes needed:**
- Differentiate response: 200 for direct add (existing user), 201 for pending invite (new user)
- Existing user path: invite record stays (accepted status, audit trail) — current behavior is correct

#### `GET /v1/organizations/invites`
**Permission:** `org:members:manage`
**Flow:**
1. Query pending invites for current org via Dapper (paginated)
2. Include inviter name, role name, expiry

**Response:** `200 OK` — `PagedResult<InviteResponse>`

#### `PATCH /v1/organizations/invites/{id}/cancel`
**Permission:** `org:members:manage`
**Flow:**
1. Verify invite exists, belongs to this org, is pending
2. Set status = `canceled`, set `canceled_by_id`
3. Revoke Clerk invitation if applicable

**Response:** `204 No Content`
**Errors:** `404 Invite.NotFound`, `409 Invite.NotCancelable`

**Changes needed:**
- Fix `cancelled` → `canceled` everywhere (entity, EF config, error messages)

---

### 2.6 Super Admin — `/v1/admin`

All admin endpoints require `RequireSystemAdministrator` (Clerk privateMetadata + DB record).

#### `GET /v1/admin/me`
**NEW ENDPOINT**
**Flow:**
1. Load system_administrators record for current user
2. Return admin profile

**Response:** `200 OK`
```json
{
  "userId": "usr_xxx",
  "email": "...",
  "firstName": "...",
  "lastName": "...",
  "adminRole": "super_admin",
  "assignedAt": "2026-01-15T00:00:00Z"
}
```

#### `GET /v1/admin/organizations`
**Flow:**
1. Query all orgs via Dapper (paginated, searchable, filterable by isActive, subscriptionPlan)

**Response:** `200 OK` — `PagedResult<AdminOrganizationListResponse>`

#### `POST /v1/admin/organizations`
**Flow:**
1. Validate name, slug uniqueness
2. Create org in DB
3. Create org in Clerk
4. Link external IDs

**Response:** `201 Created` — `{ "organizationId": "org_xxx" }`
**Errors:** `400 Validation`, `409 Organization.SlugTaken`, `502 Organization.ClerkSyncFailed`

#### `GET /v1/admin/organizations/{id}`
**Flow:**
1. Query org by public ID with full details (member count, created by, etc.)

**Response:** `200 OK` — `AdminOrganizationResponse`
**Errors:** `404 Organization.NotFound`

#### `PATCH /v1/admin/organizations/{id}`
**Flow:**
1. Validate fields
2. Load org, apply updates
3. Sync to Clerk if name/slug changed

**Response:** `204 No Content`
**Errors:** `400 Validation`, `404 Organization.NotFound`, `409 Organization.SlugTaken`

#### `PATCH /v1/admin/organizations/{id}/deactivate`
**Flow:**
1. Load org, verify active
2. Set `is_active = false`
3. Deactivate all members (`organization_users.status = 'deactivated'`)
4. Deactivate in Clerk

**Response:** `204 No Content`
**Errors:** `404 Organization.NotFound`, `409 Organization.AlreadyDeactivated`

#### `PATCH /v1/admin/organizations/{id}/reactivate`
**NEW ENDPOINT**
**Flow:**
1. Load org, verify deactivated
2. Set `is_active = true`
3. Reactivate in Clerk
4. Members stay deactivated — must be individually reactivated

**Response:** `204 No Content`
**Errors:** `404 Organization.NotFound`, `409 Organization.AlreadyActive`

#### `DELETE /v1/admin/organizations/{id}`
**NEW ENDPOINT**
**Flow:**
1. Load org
2. Hard delete from DB (CASCADE: all members, roles, invites, data gone)
3. Delete from Clerk

**Response:** `204 No Content`
**Errors:** `404 Organization.NotFound`

**Safety:** Requires confirmation header `X-Confirm-Delete: {public_id}` (e.g., `X-Confirm-Delete: org_xxx`). Returns `400` if header missing or mismatched. Irreversible CASCADE operation.

#### `GET /v1/admin/organizations/{organizationId}/members`
**Flow:**
1. Resolve org by public ID
2. Query members with roles (paginated)

**Response:** `200 OK` — `PagedResult<AdminMemberResponse>`
**Errors:** `404 Organization.NotFound`

#### `PATCH /v1/admin/organizations/{organizationId}/members/{id}/deactivate`
**NEW ENDPOINT**
**Flow:**
1. Verify membership exists and is active
2. Set `status = 'deactivated'`

**Response:** `204 No Content`
**Errors:** `404 Member.NotFound`, `409 Member.AlreadyDeactivated`

#### `PATCH /v1/admin/organizations/{organizationId}/members/{id}/reactivate`
**NEW ENDPOINT**
**Flow:**
1. Verify membership exists and is deactivated
2. Set `status = 'active'`

**Response:** `204 No Content`
**Errors:** `404 Member.NotFound`, `409 Member.AlreadyActive`

#### `DELETE /v1/admin/organizations/{organizationId}/members/{id}`
**Flow:**
1. Verify membership exists
2. Hard delete `organization_users` row (CASCADE deletes `organization_user_roles`)
3. Remove from Clerk org

**Response:** `204 No Content`
**Errors:** `404 Member.NotFound`

#### `GET /v1/admin/users`
**Flow:**
1. Query all users (paginated, searchable, filterable by isActive)

**Response:** `200 OK` — `PagedResult<AdminUserListResponse>`

#### `GET /v1/admin/users/{id}`
**Flow:**
1. Query user by public ID with org memberships

**Response:** `200 OK` — `AdminUserResponse`
**Errors:** `404 User.NotFound`

#### `PATCH /v1/admin/users/{id}/deactivate`
**Flow:**
1. Load user, verify active
2. Set `is_active = false`
3. Deactivate all org memberships
4. Suspend/ban in Clerk

**Response:** `204 No Content`
**Errors:** `404 User.NotFound`, `409 User.AlreadyDeactivated`

#### `PATCH /v1/admin/users/{id}/reactivate`
**Flow:**
1. Load user, verify deactivated
2. Set `is_active = true`
3. Reactivate in Clerk (memberships stay deactivated)

**Response:** `204 No Content`
**Errors:** `404 User.NotFound`, `409 User.AlreadyActive`

#### `DELETE /v1/admin/users/{id}`
**NEW ENDPOINT**
**Flow:**
1. Load user
2. Hard delete from DB (GDPR — FKs are SET NULL, audit attribution lost)
3. Delete from Clerk

**Response:** `204 No Content`
**Errors:** `404 User.NotFound`

**Safety:** Requires confirmation header `X-Confirm-Delete: {public_id}` (e.g., `X-Confirm-Delete: usr_xxx`). Returns `400` if header missing or mismatched. Irreversible GDPR operation.

#### `GET /v1/admin/invites`
**Flow:**
1. Query all invites system-wide (paginated, filterable by status)

**Response:** `200 OK` — `PagedResult<InviteResponse>`

#### `POST /v1/admin/organizations/{organizationId}/invites`
**Flow:** Same as org invite but admin resolves org by public ID instead of JWT context.

**Response:** `200 OK` or `201 Created`

#### `GET /v1/admin/organizations/{organizationId}/invites`
**Flow:**
1. Query invites for specific org

**Response:** `200 OK` — `PagedResult<InviteResponse>`

#### `PATCH /v1/admin/invites/{id}/cancel`
**Flow:** Same as org cancel but no org-scope restriction.

**Response:** `204 No Content`
**Errors:** `404 Invite.NotFound`, `409 Invite.NotCancelable`

**Changes needed:**
- Current admin cancel uses `DELETE` method — should be `PATCH .../cancel` for consistency

---

## 3. Issues Found in Current Code

### 3.1 Critical (Runtime Bugs)

| # | Issue | Location | Fix |
|---|-------|----------|-----|
| 1 | `Invite.Statuses.Cancelled = "cancelled"` but DB CHECK constraint is `"canceled"` | `Invite.cs:13` | Change to `"canceled"` |
| 2 | `CancelledById` property maps to `cancelled_by_id` in EF config, but DB column is `canceled_by_id` | `Invite.cs:19`, `InviteConfiguration.cs:21` | Rename property to `CanceledById`, update EF mapping |
| 2b | `CancelInviteCommand.CancelledByUserId` and handler references | `CancelInviteCommand.cs:5`, `CancelInviteHandler.cs:29,39` | Rename to `CanceledByUserId`, fix handler assignment + log |

### 3.2 American English Violations

| # | File | Current | Fix |
|---|------|---------|-----|
| 3 | `GlobalExceptionHandler.cs:81` | "cancelled" in error type URL | → "canceled" |
| 4 | `GlobalExceptionHandler.cs:84` | "cancelled by the client" | → "canceled by the client" |
| 5 | `Invite.cs:13` | `Cancelled = "cancelled"` | → `Canceled = "canceled"` |
| 6 | `Invite.cs:19` | `CancelledById` | → `CanceledById` |
| 7 | `InviteConfiguration.cs:21` | `CancelledById` → `cancelled_by_id` | → `CanceledById` → `canceled_by_id` |
| 8 | `InviteErrors.cs:26` (admin) | "can be cancelled" | → "can be canceled" |
| 8b | `CancelInviteCommand.cs:5` | `CancelledByUserId` | → `CanceledByUserId` |
| 8c | `CancelInviteHandler.cs:29` | `invite.CancelledById` | → `invite.CanceledById` |
| 8d | `CancelInviteHandler.cs:39` | `"Cancelled invite"` log message | → `"Canceled invite"` |
| 8e | Inline error in invite handler | `"Organisation is not linked to Clerk"` | → extract to error constant + "Organization" |
| 9-22 | Multiple handlers & error files | "organisation" (14 instances) | → "organization" |

### 3.3 Consistency Issues

| # | Issue | Fix |
|---|-------|-----|
| 23 | Member deactivate/reactivate use `POST` | Change to `PATCH` (state transition) |
| 24 | Admin cancel invite uses `DELETE` | Change to `PATCH .../cancel` |
| 25 | `MeResponse` includes `IsSystemAdmin` | Remove field |
| 26 | Update member roles URL is `PATCH /members/{id}` | Change to `PATCH /members/{userId}/roles` |
| 27 | `Invite.IsExpired` uses `DateTimeOffset.UtcNow` directly | Should use `IDateTimeProvider` (business logic only — infrastructure timestamps in GlobalExceptionHandler, DomainEvent are acceptable) |
| 27b | Roles list returns `IReadOnlyList<RoleResponse>`, not `PagedResult` | Convert to `PagedResult<RoleResponse>` per CLAUDE.md convention |
| 27c | Permissions endpoint returns unbounded `List<PermissionGroupResponse>` | **Acceptable exception** — permissions are system-defined and finite (~20), not user-generated data |
| 27d | `GET /members/{id}` vs `PATCH /members/{id}` — inconsistent path param naming | Standardize to `{id}` per CLAUDE.md convention (single entity = `{id}`) |
| 27e | Org-scoped invite cancel reuses `Admin.Invites.Commands.CancelInvite` | Extract shared `CancelInviteCommand` to Organizations feature area, admin reuses it |
| 27f | Self-deactivation not blocked | Add `Member.CannotDeactivateSelf` check in deactivate handlers |

### 3.4 Missing Endpoints

| # | Endpoint | Purpose |
|---|----------|---------|
| 28 | `GET /v1/admin/me` | Admin profile |
| 29 | `PATCH /v1/admin/organizations/{id}/reactivate` | Reactivate org |
| 30 | `DELETE /v1/admin/organizations/{id}` | Hard delete org |
| 31 | `PATCH /v1/admin/organizations/{orgId}/members/{id}/deactivate` | Admin deactivate member |
| 32 | `PATCH /v1/admin/organizations/{orgId}/members/{id}/reactivate` | Admin reactivate member |
| 33 | `DELETE /v1/admin/users/{id}` | Hard delete user (GDPR) |

### 3.5 Invite Flow Update

Current: Creates `Accepted` invite record for existing users (audit trail). This is correct behavior — keep it.
Change needed: Differentiate HTTP response (200 for direct add, 201 for pending invite).

### 3.6 Clerk Sync Failure Handling

When an endpoint syncs state to Clerk (deactivate org, remove member, delete user) and Clerk fails:
- **DB operation rolls back** — no partial state. Operation returns `502 ExternalService`.
- Rationale: DB + Clerk must be consistent. If Clerk is down, retry later. Never leave DB and Clerk out of sync.
- Exception: Hard deletes (GDPR `DELETE /admin/users/{id}`) — if DB delete succeeds but Clerk fails, log error and return success. GDPR compliance takes priority over Clerk sync.

---

## 4. Route Summary (Before → After)

| Current Route | Method | New Route | Method | Change |
|---------------|--------|-----------|--------|--------|
| `GET /v1/me` | GET | `GET /v1/me` | GET | Remove `isSystemAdmin` from response |
| — | — | `GET /v1/admin/me` | GET | **NEW** |
| `POST .../members/{userId}/deactivate` | POST | `PATCH .../members/{id}/deactivate` | PATCH | Method + param |
| `POST .../members/{userId}/reactivate` | POST | `PATCH .../members/{id}/reactivate` | PATCH | Method + param |
| `PATCH .../members/{userId}` | PATCH | `PATCH .../members/{id}/roles` | PATCH | URL + param |
| `GET .../members/{id}` | GET | `GET .../members/{id}` | GET | No change |
| `DELETE /v1/admin/invites/{id}` | DELETE | `PATCH /v1/admin/invites/{id}/cancel` | PATCH | Method + URL |
| — | — | `PATCH /v1/admin/organizations/{id}/reactivate` | PATCH | **NEW** |
| — | — | `DELETE /v1/admin/organizations/{id}` | DELETE | **NEW** |
| — | — | `PATCH /v1/admin/.../members/{id}/deactivate` | PATCH | **NEW** |
| — | — | `PATCH /v1/admin/.../members/{id}/reactivate` | PATCH | **NEW** |
| — | — | `DELETE /v1/admin/users/{id}` | DELETE | **NEW** |

---

## 5. CLAUDE.md Updates Needed

### 5.1 Endpoint Flow Documentation Convention

Add to CLAUDE.md: requirement that every endpoint must have flow documentation in the spec file, and any new endpoint or endpoint modification must update the documentation.

### 5.2 Lifecycle Rules

Add to CLAUDE.md: the deactivate/delete matrix and rules for orgs, users, and memberships.

### 5.3 Invite Flow Rules

Add to CLAUDE.md: the two-path invite logic (existing user → direct add, new user → invite record).

### 5.4 HTTP Method Convention Update

Clarify in CLAUDE.md: `PATCH` for all state transitions including deactivate/reactivate. `DELETE` only for hard delete of records.

---

## 6. Implementation Priority

1. **Critical fixes** — American English + runtime bugs (invite status mismatch)
2. **Remove `isSystemAdmin` from `/me`** — security/design alignment
3. **HTTP method corrections** — POST → PATCH for state transitions
4. **Invite flow redesign** — existing user direct add, new user invite
5. **Missing admin endpoints** — reactivate org, delete org/user, member deactivate/reactivate
6. **`GET /v1/admin/me`** — new endpoint
7. **CLAUDE.md updates** — conventions, flow docs, lifecycle rules
8. **Role deletion → Viewer fallback** — verify/implement
