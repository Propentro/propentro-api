# Organization Members

## GET /v1/organizations/members

Lists members of the current organization with optional filtering.

**Auth:** `org:members:read` permission

**Validation:**
- `Page` — optional, default 1, must be >= 1
- `PageSize` — optional, default 25, must be 1-100
- `Search` — optional, searches email, first name, last name
- `Status` — optional, must be `active` or `deactivated`
- `RoleId` — optional, format `^rol_[0-9a-f]{12}$`

**Business Logic:**
1. Query members in current organization with optional filters
2. Fetch roles for each member
3. Return paginated results ordered by creation date descending

**Response:** 200 OK

## GET /v1/organizations/members/{id}

Returns details for a specific member in the current organization.

**Auth:** `org:members:read` permission

**Business Logic:**
1. Query member by user public ID in current organization
2. Guard: member not found → `Member.NotFound`
3. Fetch member's roles

**Response:** 200 OK | 404

## PATCH /v1/organizations/members/{id}/roles

Updates the roles assigned to a member.

**Auth:** `org:members:manage` permission

**Validation:**
- `UserId` — required, format `^usr_[0-9a-f]{12}$`
- `RoleIds` — required, max 20 items, each format `^rol_[0-9a-f]{12}$`

**Business Logic:**
1. Resolve user by public ID
2. Guard: user not found → `Member.NotFound`
3. Resolve membership in current organization
4. Guard: not a member → `Member.UserNotMember`
5. Guard: member is deactivated → `Member.AlreadyDeactivated`
6. Validate all role IDs exist and are accessible (system or org-scoped)
7. Guard: invalid role IDs → `Member.InvalidRoleIds`
8. Guard: removing Owner role when user is the last owner → `Member.CannotRemoveLastOwner`
9. Replace all role assignments
10. Invalidate permission cache

**Events:** `MemberRolesUpdatedEvent`

**Response:** 204 No Content | 400 / 404 / 409

## PATCH /v1/organizations/members/{id}/deactivate

Deactivates a member in the current organization.

**Auth:** `org:members:manage` permission

**Validation:**
- `UserId` — required, format `^usr_[0-9a-f]{12}$`

**Business Logic:**
1. Resolve user by public ID
2. Guard: user not found → `Member.NotFound`
3. Resolve membership in current organization
4. Guard: not a member → `Member.UserNotMember`
5. Guard: already deactivated → `Member.AlreadyDeactivated`
6. Guard: target is self → `Member.CannotDeactivateSelf`
7. Guard: user is last owner → `Member.CannotRemoveLastOwner`
8. Sync to auth provider: remove org membership
9. Guard: sync fails → `Member.SyncFailed`
10. Set membership status to deactivated
11. If user has no other active memberships, deactivate user
12. Invalidate permission cache

**Events:** `MemberDeactivatedEvent`

**Response:** 204 No Content | 400 / 403 / 404 / 409 / 502

## PATCH /v1/organizations/members/{id}/reactivate

Reactivates a deactivated member in the current organization.

**Auth:** `org:members:manage` permission

**Validation:**
- `UserId` — required, format `^usr_[0-9a-f]{12}$`

**Business Logic:**
1. Resolve user by public ID
2. Guard: user not found → `Member.NotFound`
3. Resolve membership in current organization
4. Guard: not a member → `Member.UserNotMember`
5. Guard: already active → `Member.AlreadyActive`
6. Set membership status to active
7. Reactivate user
8. Sync to auth provider: add org membership (best-effort, logged on failure)
9. Invalidate permission cache

**Events:** `MemberReactivatedEvent`

**Response:** 204 No Content | 400 / 404 / 409
