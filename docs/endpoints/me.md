# Me

## GET /v1/me

Returns the authenticated user's profile including organization context, roles, and permissions.

**Auth:** Authenticated

**Business Logic:**
1. Resolve user by authenticated user ID
2. Guard: user not found → `Me.UserNotFound`
3. Fetch roles assigned to user in current organization
4. Fetch user permissions for current organization
5. Return profile with organization context, roles, and permissions

**Response:** 200 OK | 404
