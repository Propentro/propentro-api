# Organization Invites

## POST /v1/organizations/invites

Invites a user to the current organization.

**Auth:** `org:members:manage` permission

**Business Logic:**
1. Resolve organization by ID
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

**Response:** 200 OK (direct add) / 201 Created (new invite) | 404 / 409 / 502

## GET /v1/organizations/invites

Lists invites in the current organization.

**Auth:** `org:members:manage` permission

**Validation:**
- `Page` — optional, default 1
- `PageSize` — optional, default 25

**Business Logic:**
1. Query invites for the current organization
2. Return paginated results ordered by creation date descending

**Response:** 200 OK

## PATCH /v1/organizations/invites/{id}/cancel

Cancels a pending invite in the current organization.

**Auth:** `org:members:manage` permission

**Business Logic:**
1. Resolve invite by public ID
2. Guard: invite not found → `Invite.NotFound`
3. Guard: invite does not belong to current organization → `Invite.NotFound`
4. Guard: invite status is not Pending → `Invite.NotCancellable`
5. Set status to Canceled, record canceling user
6. Revoke invitation in auth provider (if org is linked)
7. Add outbox event

**Events:** `InviteCanceledEvent`

**Response:** 200 OK | 404 / 409
