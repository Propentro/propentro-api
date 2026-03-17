# Admin Members

## GET /v1/admin/organizations/{organizationId}/members

Lists members of a specific organization.

**Auth:** System Administrator

**Validation:**
- `Page` — optional, default 1
- `PageSize` — optional, default 25

**Business Logic:**
1. Query members in the specified organization with their roles
2. Return paginated results

**Response:** 200 OK

## DELETE /v1/admin/organizations/{organizationId}/members/{id}

Permanently removes a member from an organization.

**Auth:** System Administrator

**Validation:**
- `OrganizationId` — required
- `UserId` — required

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found → `AdminUser.OrganizationNotFound`
3. Resolve user by public ID
4. Guard: user not found → `AdminUser.NotFound`
5. Resolve membership
6. Guard: not a member → `AdminUser.NotAMember`
7. Hard delete all role assignments for this membership
8. Hard delete the membership record
9. Sync to auth provider: remove member
10. Guard: sync fails → `AdminUser.SyncFailed`
11. If user has no other memberships, deactivate user
12. Invalidate permission cache

**Events:** `MemberRemovedEvent`

**Response:** 200 OK | 404 / 502

## PATCH /v1/admin/organizations/{organizationId}/members/{id}/deactivate

Deactivates a member in a specific organization.

**Auth:** System Administrator

**Validation:**
- `OrganizationId` — required, format `^org_[0-9a-f]{12}$`
- `UserId` — required, format `^usr_[0-9a-f]{12}$`

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found → `AdminUser.OrganizationNotFound`
3. Resolve user by public ID
4. Guard: user not found → `AdminUser.NotFound`
5. Resolve membership
6. Guard: not a member → `AdminUser.NotAMember`
7. Guard: already deactivated → `Member.AlreadyDeactivated`
8. Set membership status to deactivated
9. If user has no other active memberships, deactivate user
10. Invalidate permission cache

**Events:** `MemberDeactivatedEvent`

**Response:** 200 OK | 400 / 404 / 409

## PATCH /v1/admin/organizations/{organizationId}/members/{id}/reactivate

Reactivates a deactivated member in a specific organization.

**Auth:** System Administrator

**Validation:**
- `OrganizationId` — required, format `^org_[0-9a-f]{12}$`
- `UserId` — required, format `^usr_[0-9a-f]{12}$`

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found → `AdminUser.OrganizationNotFound`
3. Resolve user by public ID
4. Guard: user not found → `AdminUser.NotFound`
5. Resolve membership
6. Guard: not a member → `AdminUser.NotAMember`
7. Guard: already active → `Member.AlreadyActive`
8. Set membership status to active
9. Reactivate user
10. Invalidate permission cache

**Events:** `MemberReactivatedEvent`

**Response:** 200 OK | 400 / 404 / 409
