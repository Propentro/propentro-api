# Cache Invalidation via ICacheInvalidating

**Date:** 2026-03-17
**Status:** Approved

## What

Activate the unused `ICacheInvalidating` interface across all org-scoped commands, fix the permission cache key format to align with the query cache convention, remove manual cache invalidation from handlers, and clean up `IPermissionService` by removing invalidation methods (SRP fix). Admin commands retain manual invalidation due to the absence of org context.

## Why

1. **Race condition:** All current cache invalidation happens inside handlers (pre-commit). Another request can re-populate the cache with stale data before the transaction commits. `ICacheInvalidating` runs post-commit via `UnitOfWorkBehavior`.
2. **SRP violation:** Handlers inject `ICacheService` and `IPermissionService` solely to clear cache — this is not their responsibility.
3. **Bug:** Role mutation handlers clear `org:{OrgId}:roles` but the actual cached keys are `org:{OrgId}:roles-{Page}-{PageSize}`. Cache is never actually invalidated.
4. **Missing invalidation:** `UpdateOrganizationHandler` and `ReactivateOrganizationHandler` (admin) don't invalidate the org-settings cache for the target org.

## Design Decisions

### 1. Wildcard Detection in UnitOfWorkBehavior

`InvalidateCacheAsync` currently only calls `RemoveAsync` (exact key). Add wildcard detection:

```csharp
// If key contains '*', use pattern-based removal
if (cacheKey.Contains('*'))
    await cacheService.RemoveByPatternAsync(cacheKey, cancellationToken);
else
    await cacheService.RemoveAsync(cacheKey, cancellationToken);
```

No new interface members needed. The `*` convention is universal (Redis SCAN, glob patterns).

### 2. Permission Cache Key Format Change

**Current:** `permissions:{orgId}:{userId}` — doesn't follow the `org:{OrgId}:` prefix convention.

**New:** `org:{orgId}:user-permissions:{userId}` — aligns with all query cache keys.

This means org-scoped commands can use `ICacheInvalidating` with `KeysAreFullyQualified = false` and key `"user-permissions:*"`, which the behavior automatically prefixes to `org:{OrgId}:user-permissions:*`.

Changes in `PermissionService`:
- `_cacheKeyPrefix` removed
- `GetCacheKey` → `$"org:{organizationId}:user-permissions:{userId}"`
- `InvalidatePermissionsCacheAsync` → **removed**
- `InvalidateOrganizationPermissionsCacheAsync` → **removed**
- `IPermissionService` interface drops both invalidation methods

### 3. Admin Commands: Manual Invalidation Stays

Super admin has no `userContext.OrganizationId`. The `ICacheInvalidating` prefix mechanism (`org:{OrgId}:`) cannot work without an org context. Admin commands resolve target org GUIDs at runtime in the handler.

Admin commands will call `cache.RemoveByPatternAsync(...)` directly with the new key format. This is acceptable because:
- ~9 admin commands total, rarely called
- No concurrent traffic on admin panel
- Pre-commit race condition risk is negligible for admin operations
- For destructive operations like `DeleteOrganization`, the org's Clerk external ID is revoked first, preventing new authenticated requests from entering the system before the cache clears naturally via TTL

### 4. GetPermissionsQuery — No Invalidation Needed

`org:{OrgId}:permissions` caches the permissions catalog (all available permission codes). This is seeded reference data that never changes at runtime. 1-hour TTL is sufficient.

## Cached Queries (Reference)

| Query | Cache Key | Full Key | TTL |
|---|---|---|---|
| `GetOrganizationRolesQuery` | `roles-{Page}-{PageSize}` | `org:{OrgId}:roles-{Page}-{PageSize}` | 5 min |
| `GetOrganizationSettingsQuery` | `org-settings` | `org:{OrgId}:org-settings` | 10 min |
| `GetPermissionsQuery` | `permissions` | `org:{OrgId}:permissions` | 1 hour |

### 5. Org-Wide Permission Invalidation (Deliberate Trade-off)

Commands like `DeactivateMemberCommand` and `UpdateMemberRolesCommand` affect a single user's permissions, but `ICacheInvalidating` uses static keys — the specific `userId` is only known at handler runtime. Rather than introducing a dynamic key mechanism (collector pattern, mutable commands), we use `"user-permissions:*"` which clears all users' permission caches in the org.

This is acceptable because:
- An org with 100 users = 100 cache misses on next request, each resolved by a fast indexed SQL query (~2-3ms)
- Permission cache TTL is 5 minutes — cache misses are normal operation
- The alternative (collector pattern) adds a new scoped service, interface, and DI registration for negligible performance gain

### 6. DeactivateUser / ReactivateUser — No Cache Invalidation Needed

These admin handlers only flip `user.IsActive` at the platform level. The permission query (`PermissionService.GetUserPermissionsAsync`) joins `organization_user_roles` and checks `r.is_active` (the role's active flag), not `users.is_active`. A deactivated user is rejected by the `ActiveUserAuthorizationHandler` before any permission check occurs, so stale permission cache entries are never served.

## Org-Scoped Command Mapping

Each command lists the `CacheKeysToInvalidate` values (with `KeysAreFullyQualified = false`):

| Command | Keys to Invalidate | Reason |
|---|---|---|
| `CreateRoleCommand` | `["roles-*"]` | New role affects paginated role listings |
| `UpdateRoleCommand` | `["roles-*", "user-permissions:*"]` | Role name/permissions change affects listings and user permissions |
| `DeleteRoleCommand` | `["roles-*", "user-permissions:*"]` | Deleted role affects listings and user permissions |
| `UpdateOrganizationSettingsCommand` | `["org-settings"]` | Settings changed |
| `DeactivateMemberCommand` | `["user-permissions:*"]` | Deactivated member should lose permissions |
| `ReactivateMemberCommand` | `["user-permissions:*"]` | Reactivated member should regain permissions |
| `UpdateMemberRolesCommand` | `["user-permissions:*"]` | Role assignment change affects permissions |

Commands with no cache impact (no `ICacheInvalidating`):
- `CancelInviteCommand` — invites are not cached
- `InviteToCurrentOrgCommand` — invites are not cached

## Admin Command Mapping (Manual Invalidation)

| Handler | Cache Keys to Clear | Method |
|---|---|---|
| `DeactivateOrganizationHandler` | `org:{targetOrgId}:*` | `RemoveByPatternAsync` — clears all cache for the org |
| `ReactivateOrganizationHandler` | `org:{targetOrgId}:org-settings` | `RemoveAsync` — org-settings may be stale |
| `DeleteOrganizationHandler` | `org:{targetOrgId}:*` | `RemoveByPatternAsync` — clears all cache for the org |
| `UpdateOrganizationHandler` | `org:{targetOrgId}:org-settings` | `RemoveAsync` — org details changed |
| `AdminDeactivateMemberHandler` | `org:{targetOrgId}:user-permissions:*` | `RemoveByPatternAsync` |
| `AdminReactivateMemberHandler` | `org:{targetOrgId}:user-permissions:*` | `RemoveByPatternAsync` |
| `RemoveMemberHandler` | `org:{targetOrgId}:user-permissions:*` | `RemoveByPatternAsync` |
| `DeleteUserHandler` | `org:{orgId}:user-permissions:*` per org | `RemoveByPatternAsync` looped per membership |

Admin commands with no cache impact:
- `CreateOrganizationCommand` — new org has no cached data
- `InviteUserCommand` — invites are not cached
- `DeactivateUserCommand` / `ReactivateUserCommand` — flips `user.IsActive` only; permission query doesn't check this field, and auth middleware blocks deactivated users before permission checks (see Design Decision #6)

## Webhook Command

`HandleMembershipCreatedCommand` creates `OrganizationUserRole` entries. However, the newly created member has no existing cached data (first-time member), so no invalidation is needed. The roles listing cache (`roles-*`) only lists role definitions, not role assignments, so it is unaffected.

**Re-invite edge case:** If a user was previously hard-removed (`RemoveMember`) and re-invited, their permission cache was already invalidated on removal. The new membership webhook creates fresh data with no stale cache to clear.

## Files to Modify

### Infrastructure Changes
1. **`UnitOfWorkBehavior.cs`** — Add `*` wildcard detection in `InvalidateCacheAsync`
2. **`PermissionService.cs`** — Change key format to `org:{orgId}:user-permissions:{userId}`, remove invalidation methods
3. **`IPermissionService.cs`** — Remove `InvalidatePermissionsCacheAsync` and `InvalidateOrganizationPermissionsCacheAsync`

### Org-Scoped Commands (Add ICacheInvalidating)
4. **`CreateRoleCommand.cs`** — Implement `ICacheInvalidating`
5. **`UpdateRoleCommand.cs`** — Implement `ICacheInvalidating`
6. **`DeleteRoleCommand.cs`** — Implement `ICacheInvalidating`
7. **`UpdateOrganizationSettingsCommand.cs`** — Implement `ICacheInvalidating`
8. **`DeactivateMemberCommand.cs`** — Implement `ICacheInvalidating`
9. **`ReactivateMemberCommand.cs`** — Implement `ICacheInvalidating`
10. **`UpdateMemberRolesCommand.cs`** — Implement `ICacheInvalidating`

### Org-Scoped Handlers (Remove Manual Cache Code)
11. **`CreateRoleHandler.cs`** — Remove `ICacheService` injection and `cache.RemoveAsync` call
12. **`UpdateRoleHandler.cs`** — Remove `ICacheService` + `IPermissionService` injection and cache calls
13. **`DeleteRoleHandler.cs`** — Remove `ICacheService` + `IPermissionService` injection and cache calls
14. **`UpdateOrganizationSettingsHandler.cs`** — Remove `ICacheService` injection and `cache.RemoveAsync` call
15. **`DeactivateMemberHandler.cs`** — Remove `IPermissionService` injection and invalidation call
16. **`ReactivateMemberHandler.cs`** — Remove `IPermissionService` injection and invalidation call
17. **`UpdateMemberRolesHandler.cs`** — Remove `IPermissionService` injection and invalidation call

### Admin Handlers (Update Key Format + Add Missing Invalidation)
18. **`DeactivateOrganizationHandler.cs`** — Replace `permissionService.InvalidateOrganizationPermissionsCacheAsync` with `cache.RemoveByPatternAsync($"org:{org.Id}:*")`
19. **`DeleteOrganizationHandler.cs`** — Same pattern as above
20. **`UpdateOrganizationHandler.cs`** — Add `cache.RemoveAsync($"org:{org.Id}:org-settings")` (currently missing)
21. **`ReactivateOrganizationHandler.cs`** — Add `cache.RemoveAsync($"org:{org.Id}:org-settings")` (currently missing)
22. **`AdminDeactivateMemberHandler.cs`** — Replace `permissionService` call with `cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*")`
23. **`AdminReactivateMemberHandler.cs`** — Same pattern
24. **`RemoveMemberHandler.cs`** — Same pattern
25. **`DeleteUserHandler.cs`** — Replace looped `permissionService` calls with looped `cache.RemoveByPatternAsync`

### CLAUDE.md Update
26. **`CLAUDE.md`** — Add convention: admin commands use manual cache invalidation with `ICacheService` directly (no `ICacheInvalidating`) because super admin has no org context

## Testing Strategy

- **Existing integration tests** cover the full pipeline (HTTP → MediatR → DB → response). Since `ICacheInvalidating` is processed by the same `UnitOfWorkBehavior` already in the pipeline, existing tests implicitly verify the behavior.
- **Architecture test** (optional): Add a test that verifies all org-scoped `ICommand` implementations that should have `ICacheInvalidating` actually implement it.
- **No new unit tests needed** for `UnitOfWorkBehavior` wildcard detection — the existing pipeline integration tests exercise this path.

## Out of Scope

- Stampede protection for `QueryCachingBehavior` (uses `GetAsync`, not `GetOrCreateAsync`)
- Cache key versioning or tagging
- Distributed cache invalidation (pub/sub)
- Collector pattern for dynamic cache keys (YAGNI — admin commands handle this manually)
