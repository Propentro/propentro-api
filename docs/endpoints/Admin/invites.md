# Admin Invites

## GET /v1/admin/invites

Lists all invites across organizations.

**Auth:** System Administrator

**Validation:**
- `Status` — optional, defaults to `pending`
- `Page` — optional, default 1
- `PageSize` — optional, default 25

**Business Logic:**
1. Query invites filtered by status
2. Return paginated results ordered by creation date descending

**Response:** 200 OK

## POST /v1/admin/organizations/{organizationId}/invites

Invites a user to a specific organization.

**Auth:** System Administrator

**Validation:**
- `OrganizationId` — required
- `Email` — required, valid email format
- `RoleId` — required

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found or inactive → `Invite.OrganizationNotFound`
3. Guard: organization not linked to auth provider → `Organization.NotLinkedToAuthProvider`
4. Resolve role by public ID (must be active, system or org-scoped)
5. Guard: role not found → `Invite.RoleNotFound`
6. Check if user with email already exists
7. **Existing user path:**
   - Guard: already a member of the organization → `Invite.AlreadyMember`
   - Reactivate user if inactive
   - Create organization membership (active) and assign role
   - Sync membership to auth provider
   - Guard: sync fails → `Invite.SyncFailed`
   - Create invite record with status Accepted (audit trail)
8. **New user path:**
   - Guard: pending invite already exists for this email → `Invite.AlreadyPending`
   - Send invitation email via auth provider
   - Guard: sync fails → `Invite.SyncFailed`
   - Create invite record with status Pending, expires in 7 days

**Response:** 200 OK (direct add) / 201 Created (new invite) | 400 / 404 / 409 / 502

## GET /v1/admin/organizations/{organizationId}/invites

Lists invites for a specific organization.

**Auth:** System Administrator

**Validation:**
- `Page` — optional, default 1
- `PageSize` — optional, default 25

**Business Logic:**
1. Query invites for the specified organization
2. Return paginated results ordered by creation date descending

**Response:** 200 OK

## PATCH /v1/admin/invites/{id}/cancel

Cancels a pending invite.

**Auth:** System Administrator

**Business Logic:**
1. Resolve invite by public ID
2. Guard: invite not found → `Invite.NotFound`
3. Guard: invite status is not Pending → `Invite.NotCancellable`
4. Set status to Canceled, record canceling user
5. Revoke invitation in auth provider (if org is linked)
6. Add outbox event

**Events:** `InviteCanceledEvent`

**Response:** 200 OK | 404 / 409
