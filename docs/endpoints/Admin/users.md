# Admin Users

## GET /v1/admin/users

Lists all users with optional filtering.

**Auth:** System Administrator

**Validation:**
- `Search` — optional, searches email, first name, last name
- `IsActive` — optional, filters by active status
- `Page` — optional, default 1
- `PageSize` — optional, default 25

**Business Logic:**
1. Query users with optional search and active status filters
2. Count organizations per user
3. Return paginated results ordered by creation date descending

**Response:** 200 OK

## GET /v1/admin/users/{id}

Returns user details with all organization memberships and roles.

**Auth:** System Administrator

**Business Logic:**
1. Query user by public ID
2. Guard: user not found → `AdminUser.NotFound`
3. Fetch all organization memberships with roles

**Response:** 200 OK | 404

## PATCH /v1/admin/users/{id}/deactivate

Deactivates a user account.

**Auth:** System Administrator

**Validation:**
- `Id` — required

**Business Logic:**
1. Resolve user by public ID
2. Guard: user not found → `AdminUser.NotFound`
3. Guard: already inactive → `AdminUser.AlreadyInactive`
4. Set user as inactive

**Response:** 200 OK | 404 / 409

## PATCH /v1/admin/users/{id}/reactivate

Reactivates a deactivated user account.

**Auth:** System Administrator

**Validation:**
- `Id` — required

**Business Logic:**
1. Resolve user by public ID
2. Guard: user not found → `AdminUser.NotFound`
3. Guard: already active → `AdminUser.AlreadyActive`
4. Set user as active

**Response:** 200 OK | 404 / 409

## DELETE /v1/admin/users/{id}

Permanently deletes a user (GDPR, irreversible).

**Auth:** System Administrator

**Validation:**
- `Id` — required, format `^usr_[0-9a-f]{12}$`
- `X-Confirm-Delete` header — required, must match the user public ID

**Business Logic:**
1. Resolve user by public ID
2. Guard: user not found → `AdminUser.NotFound`
3. Invalidate permission cache for all user memberships
4. Delete user from auth provider (best-effort, logged on failure)
5. Hard delete user from database (FK SET NULL cascade)

**Response:** 200 OK | 400 / 404
