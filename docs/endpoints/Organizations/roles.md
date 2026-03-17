# Organization Roles

## GET /v1/organizations/roles

Lists all roles for the current organization (including system roles).

**Auth:** `org:roles:read` permission

**Validation:**
- `Page` — optional, default 1
- `PageSize` — optional, default 25

**Business Logic:**
1. Query active roles scoped to current organization or system roles
2. Count members per role
3. Fetch permissions for each role
4. Return paginated results

**Response:** 200 OK

## GET /v1/organizations/roles/{id}

Returns details for a specific role.

**Auth:** `org:roles:read` permission

**Business Logic:**
1. Query role by public ID (must be org-scoped or system, and active)
2. Guard: role not found → `Role.NotFound`
3. Fetch permissions for the role
4. Return role with member count and permissions

**Response:** 200 OK | 404

## POST /v1/organizations/roles

Creates a custom role in the current organization.

**Auth:** `org:roles:manage` permission

**Validation:**
- `Name` — required, max 100 characters
- `PermissionCodes` — required (not null)

**Business Logic:**
1. Guard: role name already exists in organization → `Role.NameAlreadyExists`
2. Create role entity with generated public ID
3. Assign permissions matching provided codes
4. Invalidate roles cache

**Response:** 201 Created | 400 / 409

## PATCH /v1/organizations/roles/{roleId}

Updates a custom role.

**Auth:** `org:roles:manage` permission

**Validation:**
- `RoleId` — required, format `^rol_[0-9a-f]{12}$`
- `Name` — max 100 characters (if provided)

**Business Logic:**
1. Resolve role by public ID
2. Guard: role not found → `Role.NotFound`
3. Guard: role is a system role → `Role.CannotModifySystemRole`
4. Guard: role does not belong to current organization → `Role.NotFound`
5. If name provided and changed, check uniqueness
6. Guard: name already exists → `Role.NameAlreadyExists`
7. Update name, description, and permissions as provided
8. Invalidate roles and permissions cache

**Response:** 204 No Content | 400 / 404 / 409

## DELETE /v1/organizations/roles/{roleId}

Deletes a custom role, reassigning affected members to the Viewer role.

**Auth:** `org:roles:manage` permission

**Validation:**
- `RoleId` — required, format `^rol_[0-9a-f]{12}$`

**Business Logic:**
1. Resolve role by public ID
2. Guard: role not found or does not belong to organization → `Role.NotFound`
3. Guard: role is a system role → `Role.CannotModifySystemRole`
4. Reassign all members with this role to the Viewer system role
5. Delete role permissions
6. Soft-delete the role
7. Invalidate roles and permissions cache

**Response:** 204 No Content | 404 / 409

## GET /v1/organizations/permissions

Lists all available permissions grouped by category.

**Auth:** `org:roles:read` permission

**Business Logic:**
1. Query all permissions ordered by group and code
2. Group by category
3. Return grouped permissions

**Response:** 200 OK
