# Organization Settings

## GET /v1/organizations/settings

Returns the current organization's settings.

**Auth:** `org:settings:read` permission

**Business Logic:**
1. Resolve organization from JWT
2. Guard: organization not found or inactive → `Organization.NotFound`
3. Return organization settings

**Response:** 200 OK | 404

## PATCH /v1/organizations/settings

Updates the current organization's settings.

**Auth:** `org:settings:update` permission

**Validation:**
- `Name` — required, max 100 characters
- `ContactEmail` — valid email format (if provided)
- `ContactPhone` — max 50 characters (if provided)
- `AddressLine1` — max 255 characters (if provided)
- `AddressLine2` — max 255 characters (if provided)
- `City` — max 100 characters (if provided)
- `Region` — max 100 characters (if provided)
- `PostalCode` — max 20 characters (if provided)
- `CountryCode` — exactly 2 characters (if provided)
- `LegalName` — max 255 characters (if provided)
- `TaxNumber` — max 50 characters (if provided)
- `TaxCountry` — max 100 characters (if provided)
- `CurrencyCode` — required, exactly 3 characters
- `Timezone` — required, max 100 characters

**Business Logic:**
1. Resolve organization from JWT
2. Guard: organization not found → `Organization.NotFound`
3. If name changed and organization is linked to auth provider, sync name
4. Guard: sync fails → `Organization.SyncFailed`
5. Invalidate settings cache
6. Update organization fields

**Response:** 200 OK | 400 / 404 / 502
