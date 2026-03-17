# Admin Organizations

## GET /v1/admin/organizations

Lists all organizations with optional filtering.

**Auth:** System Administrator

**Validation:**
- `Search` — optional, filters by name or slug
- `IsActive` — optional, filters by active status
- `Page` — optional, default 1
- `PageSize` — optional, default 25

**Business Logic:**
1. Query organizations with optional search and active status filters
2. Count members per organization
3. Return paginated results ordered by creation date descending

**Response:** 200 OK

## POST /v1/admin/organizations

Creates a new organization.

**Auth:** System Administrator

**Validation:**
- `Name` — required, max 100 characters
- `Slug` — required, max 100 characters, format `^[a-z0-9-]+$`
- `ContactEmail` — valid email format (if provided)
- `CurrencyCode` — required, exactly 3 characters
- `Timezone` — required, max 100 characters
- `SubscriptionPlan` — required, must be `starter`, `pro`, or `enterprise`

**Business Logic:**
1. Guard: slug already exists → `Organization.SlugTaken`
2. Create organization entity with generated public ID
3. Sync to auth provider: create organization
4. Guard: sync fails → `Organization.SyncFailed`
5. Store external ID from auth provider

**Response:** 200 OK | 400 / 409 / 502

## GET /v1/admin/organizations/{id}

Returns organization details with member count.

**Auth:** System Administrator

**Business Logic:**
1. Query organization by public ID with member count
2. Guard: organization not found → `Organization.NotFound`

**Response:** 200 OK | 404

## PATCH /v1/admin/organizations/{id}

Updates organization details.

**Auth:** System Administrator

**Validation:**
- `Id` — required
- `Name` — required, max 200 characters
- `ContactEmail` — valid email format (if provided)
- `CurrencyCode` — required, exactly 3 characters
- `Timezone` — required

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found or inactive → `Organization.NotFound`
3. Update organization fields
4. If name changed and organization is linked to auth provider, sync name
5. Guard: sync fails → `Organization.SyncFailed`

**Response:** 200 OK | 400 / 404 / 502

## PATCH /v1/admin/organizations/{id}/deactivate

Deactivates an organization and all its members.

**Auth:** System Administrator

**Validation:**
- `Id` — required

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found → `Organization.NotFound`
3. Guard: already inactive → `Organization.AlreadyInactive`
4. Sync to auth provider: delete organization
5. Guard: sync fails → `Organization.SyncFailed`
6. Remove all member roles and memberships
7. Cancel all pending invites
8. Set organization as inactive
9. Deactivate users with no other organization memberships
10. Invalidate permission cache

**Events:** `OrganizationDeactivatedEvent`

**Response:** 200 OK | 404 / 409 / 502

## PATCH /v1/admin/organizations/{id}/reactivate

Reactivates a deactivated organization.

**Auth:** System Administrator

**Validation:**
- `Id` — required

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found → `Organization.NotFound`
3. Guard: already active → `Organization.AlreadyActive`
4. Sync to auth provider: create new organization
5. Guard: sync fails → `Organization.SyncFailed`
6. Store new external ID
7. Set organization as active

**Response:** 200 OK | 404 / 409 / 502

## DELETE /v1/admin/organizations/{id}

Permanently deletes an organization (irreversible).

**Auth:** System Administrator

**Validation:**
- `Id` — required
- `X-Confirm-Delete` header — required, must match the organization public ID

**Business Logic:**
1. Resolve organization by public ID
2. Guard: organization not found → `Organization.NotFound`
3. If linked to auth provider, sync deletion
4. Guard: sync fails → `Organization.SyncFailed`
5. Invalidate permission cache
6. Hard delete organization (cascade)

**Response:** 204 No Content | 400 / 404 / 502
