# Admin Me

## GET /v1/admin/me

Returns the authenticated system administrator's profile.

**Auth:** System Administrator

**Business Logic:**
1. Resolve system administrator record for authenticated user
2. Guard: admin record not found → `Me.AdminNotFound`
3. Return admin profile with role and assignment date

**Response:** 200 OK | 404
