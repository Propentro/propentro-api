# Cache Invalidation via ICacheInvalidating — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Activate the `ICacheInvalidating` interface on all org-scoped commands, fix permission cache key format, remove manual cache invalidation from handlers, add wildcard support to `UnitOfWorkBehavior`, and update admin handlers to use `ICacheService` directly with the new key format.

**Architecture:** Org-scoped commands declare cache keys via `ICacheInvalidating`; `UnitOfWorkBehavior` invalidates them post-commit with org prefix. Admin commands use `ICacheService` directly since they lack org context. Permission cache keys change from `permissions:{orgId}:{userId}` to `org:{orgId}:user-permissions:{userId}` to align with the query cache convention.

**Tech Stack:** .NET 10, MediatR, EF Core, Redis/MemoryCache

**Spec:** `docs/superpowers/specs/2026-03-17-cache-invalidation-design.md`

---

## File Map

| # | File | Action | Responsibility |
|---|---|---|---|
| 1 | `src/Propentro.Application/Common/Behaviors/UnitOfWorkBehavior.cs` | Modify | Add wildcard (`*`) detection in `InvalidateCacheAsync` |
| 2 | `src/Propentro.Application/Common/Interfaces/Authorization/IPermissionService.cs` | Modify | Remove `InvalidatePermissionsCacheAsync` and `InvalidateOrganizationPermissionsCacheAsync` |
| 3 | `src/Propentro.Infrastructure/Authorization/PermissionService.cs` | Modify | Change key format, remove invalidation methods and `_cacheKeyPrefix` |
| 4 | `src/Propentro.Application/Features/Organizations/Roles/Commands/CreateRole/CreateRoleCommand.cs` | Modify | Implement `ICacheInvalidating` |
| 5 | `src/Propentro.Application/Features/Organizations/Roles/Commands/CreateRole/CreateRoleHandler.cs` | Modify | Remove `ICacheService` injection and manual cache call |
| 6 | `src/Propentro.Application/Features/Organizations/Roles/Commands/UpdateRole/UpdateRoleCommand.cs` | Modify | Implement `ICacheInvalidating` |
| 7 | `src/Propentro.Application/Features/Organizations/Roles/Commands/UpdateRole/UpdateRoleHandler.cs` | Modify | Remove `IPermissionService` + `ICacheService` injection and manual cache calls |
| 8 | `src/Propentro.Application/Features/Organizations/Roles/Commands/DeleteRole/DeleteRoleCommand.cs` | Modify | Implement `ICacheInvalidating` |
| 9 | `src/Propentro.Application/Features/Organizations/Roles/Commands/DeleteRole/DeleteRoleHandler.cs` | Modify | Remove `IPermissionService` + `ICacheService` injection and manual cache calls |
| 10 | `src/Propentro.Application/Features/Organizations/Settings/Commands/UpdateOrganizationSettings/UpdateOrganizationSettingsCommand.cs` | Modify | Implement `ICacheInvalidating` |
| 11 | `src/Propentro.Application/Features/Organizations/Settings/Commands/UpdateOrganizationSettings/UpdateOrganizationSettingsHandler.cs` | Modify | Remove `ICacheService` injection and manual cache call |
| 12 | `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberCommand.cs` | Modify | Implement `ICacheInvalidating` |
| 13 | `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs` | Modify | Remove `IPermissionService` injection and manual cache call |
| 14 | `src/Propentro.Application/Features/Organizations/Members/Commands/ReactivateMember/ReactivateMemberCommand.cs` | Modify | Implement `ICacheInvalidating` |
| 15 | `src/Propentro.Application/Features/Organizations/Members/Commands/ReactivateMember/ReactivateMemberHandler.cs` | Modify | Remove `IPermissionService` injection and manual cache call |
| 16 | `src/Propentro.Application/Features/Organizations/Members/Commands/UpdateMemberRoles/UpdateMemberRolesCommand.cs` | Modify | Implement `ICacheInvalidating` |
| 17 | `src/Propentro.Application/Features/Organizations/Members/Commands/UpdateMemberRoles/UpdateMemberRolesHandler.cs` | Modify | Remove `IPermissionService` injection and manual cache call |
| 18 | `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs` | Modify | Replace `IPermissionService` with `ICacheService`, use new key format |
| 19 | `src/Propentro.Application/Features/Admin/Organizations/Commands/ReactivateOrganization/ReactivateOrganizationHandler.cs` | Modify | Add `ICacheService`, add missing org-settings invalidation |
| 20 | `src/Propentro.Application/Features/Admin/Organizations/Commands/DeleteOrganization/DeleteOrganizationHandler.cs` | Modify | Replace `IPermissionService` with `ICacheService`, use new key format |
| 21 | `src/Propentro.Application/Features/Admin/Organizations/Commands/UpdateOrganization/UpdateOrganizationHandler.cs` | Modify | Add `ICacheService`, add missing org-settings invalidation |
| 22 | `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/AdminDeactivateMemberHandler.cs` | Modify | Replace `IPermissionService` with `ICacheService`, use new key format |
| 23 | `src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/AdminReactivateMemberHandler.cs` | Modify | Replace `IPermissionService` with `ICacheService`, use new key format |
| 24 | `src/Propentro.Application/Features/Admin/Users/Commands/RemoveMember/RemoveMemberHandler.cs` | Modify | Replace `IPermissionService` with `ICacheService`, use new key format |
| 25 | `src/Propentro.Application/Features/Admin/Users/Commands/DeleteUser/DeleteUserHandler.cs` | Modify | Replace `IPermissionService` with `ICacheService`, use new key format |
| 26 | `CLAUDE.md` | Modify | Add admin cache invalidation convention |

---

### Task 1: Add wildcard detection to UnitOfWorkBehavior

**Files:**
- Modify: `src/Propentro.Application/Common/Behaviors/UnitOfWorkBehavior.cs:71-88`

- [ ] **Step 1: Add wildcard branch to `InvalidateCacheAsync`**

In the `foreach` loop body (line 85), replace the single `RemoveAsync` call with wildcard detection:

```csharp
// Current (line 83-87):
foreach (var cacheKey in cacheKeys)
{
    await cacheService.RemoveAsync(cacheKey, cancellationToken);
    Log.Debug("Cache invalidated for key {CacheKey} after {RequestName}", cacheKey, typeof(TRequest).Name);
}

// New:
foreach (var cacheKey in cacheKeys)
{
    if (cacheKey.Contains('*'))
        await cacheService.RemoveByPatternAsync(cacheKey, cancellationToken);
    else
        await cacheService.RemoveAsync(cacheKey, cancellationToken);

    Log.Debug("Cache invalidated for key {CacheKey} after {RequestName}", cacheKey, typeof(TRequest).Name);
}
```

- [ ] **Step 2: Build**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeds with 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Application/Common/Behaviors/UnitOfWorkBehavior.cs
git commit -m "feat: add wildcard cache key support to UnitOfWorkBehavior"
```

---

### Task 2: Change permission cache key format and remove invalidation methods

**Files:**
- Modify: `src/Propentro.Application/Common/Interfaces/Authorization/IPermissionService.cs`
- Modify: `src/Propentro.Infrastructure/Authorization/PermissionService.cs`

- [ ] **Step 1: Remove invalidation methods from `IPermissionService`**

Remove `InvalidatePermissionsCacheAsync` (lines 36-44) and `InvalidateOrganizationPermissionsCacheAsync` (lines 46-54). Keep only `GetUserPermissionsAsync` and `HasPermissionAsync`. The full interface becomes:

```csharp
namespace Propentro.Application.Common.Interfaces.Authorization;

/// <summary>
/// Provides permission lookup for authorization purposes.
/// Permissions are cached per user/organization to minimize database queries.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all permission codes assigned to a user within an organization.
    /// Results are cached for performance.
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission within an organization.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permissionCode,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Update `PermissionService` — change key format and remove invalidation methods**

1. Remove `_cacheKeyPrefix` field (line 13)
2. Change `GetCacheKey` (line 89-90) to: `$"org:{organizationId}:user-permissions:{userId}"`
3. Remove `InvalidatePermissionsCacheAsync` method (lines 73-80)
4. Remove `InvalidateOrganizationPermissionsCacheAsync` method (lines 82-87)

The full implementation becomes:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Authorization;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Provides permission lookup for authorization purposes.
/// Uses GetOrCreateAsync for stampede-protected caching.
/// </summary>
public sealed class PermissionService(IDbConnectionFactory db, ICacheService cache) : IPermissionService
{
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public async Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || organizationId == Guid.Empty)
        {
            return [];
        }

        var cacheKey = GetCacheKey(userId, organizationId);

        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                await using var connection = db.CreateConnection();
                await connection.OpenAsync(ct);

                const string sql = """
                    SELECT DISTINCT p.code
                    FROM organization_user_roles our
                    JOIN role_permissions rp ON rp.role_id = our.role_id
                    JOIN permissions p ON p.id = rp.permission_id
                    JOIN roles r ON r.id = our.role_id
                    WHERE our.user_id = @UserId
                      AND our.organization_id = @OrganizationId
                      AND r.is_active = TRUE
                    """;

                var permissions = await connection.QueryAsync<string>(
                    sql,
                    new { UserId = userId, OrganizationId = organizationId });

                return permissions.ToHashSet();
            },
            _cacheExpiration,
            cancellationToken);

        return result ?? [];
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        var permissions = await GetUserPermissionsAsync(userId, organizationId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    private static string GetCacheKey(Guid userId, Guid organizationId) =>
        $"org:{organizationId}:user-permissions:{userId}";
}
```

- [ ] **Step 3: Build**

Run: `dotnet build Propentro.slnx`
Expected: Build FAILS — handlers still reference removed methods. This is expected; we fix them in Tasks 3-5.

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Application/Common/Interfaces/Authorization/IPermissionService.cs src/Propentro.Infrastructure/Authorization/PermissionService.cs
git commit -m "refactor: change permission cache key format and remove invalidation methods from IPermissionService"
```

---

### Task 3: Add ICacheInvalidating to org-scoped commands and clean up handlers — Roles

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Commands/CreateRole/CreateRoleCommand.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Commands/CreateRole/CreateRoleHandler.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Commands/UpdateRole/UpdateRoleCommand.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Commands/UpdateRole/UpdateRoleHandler.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Commands/DeleteRole/DeleteRoleCommand.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Commands/DeleteRole/DeleteRoleHandler.cs`

- [ ] **Step 1: `CreateRoleCommand` — implement `ICacheInvalidating`**

```csharp
namespace Propentro.Application.Features.Organizations.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description, string[] PermissionCodes)
    : ICommand<string>, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["roles-*"];
}
```

- [ ] **Step 2: `CreateRoleHandler` — remove `ICacheService` injection and manual cache call**

Remove `ICacheService cache` from the primary constructor (line 10). Remove line 55 (`await cache.RemoveAsync(...)`). The handler becomes:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles.Commands.CreateRole;

public sealed class CreateRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateRoleCommand, string>
{
    public async Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await dbContext.Roles
            .IgnoreQueryFilters()
            .AnyAsync(
                r => r.OrganizationId == currentUser.OrganizationId
                    && r.Name == request.Name
                    && r.IsActive,
                cancellationToken);

        if (nameExists)
            return RoleErrors.NameAlreadyExists;

        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            PublicId = Domain.Common.PublicIdGenerator.Generate(Role.PublicIdPrefix),
            OrganizationId = currentUser.OrganizationId,
            Name = request.Name,
            Description = request.Description,
            IsSystem = false
        };

        dbContext.Roles.Add(role);

        if (request.PermissionCodes.Length > 0)
        {
            var permissions = await dbContext.Permissions
                .Where(p => request.PermissionCodes.Contains(p.Code))
                .ToListAsync(cancellationToken);

            foreach (var permission in permissions)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    CreatedAt = dateTimeProvider.UtcNow
                });
            }
        }

        return role.PublicId;
    }
}
```

- [ ] **Step 3: `UpdateRoleCommand` — implement `ICacheInvalidating`**

```csharp
namespace Propentro.Application.Features.Organizations.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(string RoleId, string? Name, string? Description, string[]? PermissionCodes)
    : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["roles-*", "user-permissions:*"];
}
```

- [ ] **Step 4: `UpdateRoleHandler` — remove `IPermissionService` + `ICacheService` and manual cache calls**

Remove `IPermissionService permissionService` and `ICacheService cache` from the primary constructor. Remove lines 78-79 (the two cache invalidation calls). Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The handler becomes:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles.Commands.UpdateRole;

public sealed class UpdateRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.PublicId == request.RoleId && r.IsActive, cancellationToken);

        if (role is null)
            return RoleErrors.NotFound;

        if (role.IsSystem)
            return RoleErrors.CannotModifySystemRole;

        if (role.OrganizationId != currentUser.OrganizationId)
            return RoleErrors.NotFound;

        if (request.Name is not null && request.Name != role.Name)
        {
            var nameExists = await dbContext.Roles
                .IgnoreQueryFilters()
                .AnyAsync(
                    r => r.OrganizationId == currentUser.OrganizationId
                        && r.Name == request.Name
                        && r.IsActive
                        && r.Id != role.Id,
                    cancellationToken);

            if (nameExists)
                return RoleErrors.NameAlreadyExists;

            role.Name = request.Name;
        }

        if (request.Description is not null)
            role.Description = request.Description;

        if (request.PermissionCodes is not null)
        {
            var existingPermissions = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingPermissions)
                dbContext.RolePermissions.Remove(existing);

            if (request.PermissionCodes.Length > 0)
            {
                var permissions = await dbContext.Permissions
                    .Where(p => request.PermissionCodes.Contains(p.Code))
                    .ToListAsync(cancellationToken);

                foreach (var permission in permissions)
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        CreatedAt = dateTimeProvider.UtcNow
                    });
                }
            }
        }

        return Result.Success();
    }
}
```

- [ ] **Step 5: `DeleteRoleCommand` — implement `ICacheInvalidating`**

```csharp
namespace Propentro.Application.Features.Organizations.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(string RoleId) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["roles-*", "user-permissions:*"];
}
```

- [ ] **Step 6: `DeleteRoleHandler` — remove `IPermissionService` + `ICacheService` and manual cache calls**

Remove `IPermissionService permissionService` and `ICacheService cache` from the primary constructor. Remove lines 75-76 (the two cache invalidation calls). Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The handler becomes:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles.Commands.DeleteRole;

public sealed class DeleteRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.PublicId == request.RoleId && r.IsActive, cancellationToken);

        if (role is null || role.OrganizationId != currentUser.OrganizationId)
            return RoleErrors.NotFound;

        if (role.IsSystem)
            return RoleErrors.CannotModifySystemRole;

        var memberRoleAssignments = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .Where(our => our.OrganizationId == currentUser.OrganizationId && our.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        if (memberRoleAssignments.Count > 0)
        {
            var viewerRole = await dbContext.Roles
                .IgnoreQueryFilters()
                .FirstAsync(r => r.Name == "Viewer" && r.IsSystem && r.IsActive, cancellationToken);

            var affectedUserIds = memberRoleAssignments.Select(a => a.UserId).Distinct().ToList();

            var existingViewerAssignments = await dbContext.OrganizationUserRoles
                .IgnoreQueryFilters()
                .Where(our => our.OrganizationId == currentUser.OrganizationId
                    && our.RoleId == viewerRole.Id
                    && affectedUserIds.Contains(our.UserId))
                .Select(our => our.UserId)
                .ToListAsync(cancellationToken);

            var usersNeedingViewer = affectedUserIds.Except(existingViewerAssignments).ToList();

            foreach (var userId in usersNeedingViewer)
            {
                dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
                {
                    OrganizationId = currentUser.OrganizationId,
                    UserId = userId,
                    RoleId = viewerRole.Id,
                    AssignedAt = dateTimeProvider.UtcNow,
                    AssignedBy = currentUser.UserId
                });
            }

            foreach (var assignment in memberRoleAssignments)
                dbContext.OrganizationUserRoles.Remove(assignment);
        }

        var rolePermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        foreach (var rp in rolePermissions)
            dbContext.RolePermissions.Remove(rp);

        role.IsActive = false;

        return Result.Success();
    }
}
```

- [ ] **Step 7: Build**

Run: `dotnet build Propentro.slnx`
Expected: Build FAILS — member handlers and admin handlers still reference removed `IPermissionService` methods. Expected; fixed in Tasks 4-5.

- [ ] **Step 8: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Roles/
git commit -m "feat: add ICacheInvalidating to role commands and remove manual cache code from handlers"
```

---

### Task 4: Add ICacheInvalidating to org-scoped commands and clean up handlers — Settings & Members

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Settings/Commands/UpdateOrganizationSettings/UpdateOrganizationSettingsCommand.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Settings/Commands/UpdateOrganizationSettings/UpdateOrganizationSettingsHandler.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberCommand.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/ReactivateMember/ReactivateMemberCommand.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/ReactivateMember/ReactivateMemberHandler.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/UpdateMemberRoles/UpdateMemberRolesCommand.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/UpdateMemberRoles/UpdateMemberRolesHandler.cs`

- [ ] **Step 1: `UpdateOrganizationSettingsCommand` — implement `ICacheInvalidating`**

```csharp
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Application.Features.Organizations.Settings.Commands.UpdateOrganizationSettings;

public sealed record UpdateOrganizationSettingsCommand(
    string Name,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? LegalName,
    string? TaxNumber,
    string? TaxCountry,
    string CurrencyCode,
    string Timezone) : ICommand<OrganizationSettingsResponse>, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["org-settings"];
}
```

- [ ] **Step 2: `UpdateOrganizationSettingsHandler` — remove `ICacheService` injection and manual cache call**

Remove `ICacheService cache` from the primary constructor (line 13). Remove line 43 (`await cache.RemoveAsync(...)`). The handler becomes:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Application.Features.Organizations.Settings.Commands.UpdateOrganizationSettings;

public sealed class UpdateOrganizationSettingsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkOrganizationService) : ICommandHandler<UpdateOrganizationSettingsCommand, OrganizationSettingsResponse>
{
    public async Task<Result<OrganizationSettingsResponse>> Handle(UpdateOrganizationSettingsCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        var nameChanged = !string.Equals(org.Name, request.Name, StringComparison.Ordinal);

        if (nameChanged && !string.IsNullOrEmpty(org.ExternalId))
        {
            var syncResult = await clerkOrganizationService.UpdateOrganizationAsync(
                org.ExternalId,
                request.Name,
                cancellationToken);

            if (syncResult.IsFailure)
            {
                Log.Warning(
                    "Failed to sync organization name to Clerk for {OrganizationId}: {Error}",
                    org.PublicId,
                    syncResult.Error.Message);

                return OrganizationErrors.SyncFailed;
            }
        }

        org.Name = request.Name;
        org.ContactName = request.ContactName;
        org.ContactEmail = request.ContactEmail;
        org.ContactPhone = request.ContactPhone;
        org.AddressLine1 = request.AddressLine1;
        org.AddressLine2 = request.AddressLine2;
        org.City = request.City;
        org.Region = request.Region;
        org.PostalCode = request.PostalCode;
        org.CountryCode = request.CountryCode;
        org.LegalName = request.LegalName;
        org.TaxNumber = request.TaxNumber;
        org.TaxCountry = request.TaxCountry;
        org.CurrencyCode = request.CurrencyCode;
        org.Timezone = request.Timezone;

        return new OrganizationSettingsResponse
        {
            OrganizationId = org.PublicId,
            Name = org.Name,
            Slug = org.Slug,
            ContactName = org.ContactName,
            ContactEmail = org.ContactEmail,
            ContactPhone = org.ContactPhone,
            AddressLine1 = org.AddressLine1,
            AddressLine2 = org.AddressLine2,
            City = org.City,
            Region = org.Region,
            PostalCode = org.PostalCode,
            CountryCode = org.CountryCode,
            LegalName = org.LegalName,
            TaxNumber = org.TaxNumber,
            TaxCountry = org.TaxCountry,
            CurrencyCode = org.CurrencyCode,
            Timezone = org.Timezone
        };
    }
}
```

- [ ] **Step 3: `DeactivateMemberCommand` — implement `ICacheInvalidating`**

```csharp
namespace Propentro.Application.Features.Organizations.Members.Commands.DeactivateMember;

public sealed record DeactivateMemberCommand(string UserId) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["user-permissions:*"];
}
```

- [ ] **Step 4: `DeactivateMemberHandler` — remove `IPermissionService` and manual cache call**

Remove `IPermissionService permissionService` from the primary constructor (line 13). Remove line 85 (`await permissionService.InvalidatePermissionsCacheAsync(...)`). Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The handler becomes:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.Commands.DeactivateMember;

public sealed class DeactivateMemberHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkService) : ICommandHandler<DeactivateMemberCommand>
{
    public async Task<Result> Handle(DeactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId && u.IsActive, cancellationToken);

        if (user is null)
            return MemberErrors.NotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == currentUser.OrganizationId && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return MemberErrors.UserNotMember;

        if (membership.Status == OrganizationUser.Statuses.Deactivated)
            return MemberErrors.AlreadyDeactivated;

        if (user.Id == currentUser.UserId)
            return MemberErrors.CannotDeactivateSelf;

        var ownerRole = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "Owner" && r.IsSystem && r.IsActive, cancellationToken);

        if (ownerRole is not null)
        {
            var userIsOwner = await dbContext.OrganizationUserRoles
                .IgnoreQueryFilters()
                .AnyAsync(our => our.OrganizationId == currentUser.OrganizationId
                    && our.UserId == user.Id
                    && our.RoleId == ownerRole.Id,
                    cancellationToken);

            if (userIsOwner)
            {
                var ownerCount = await dbContext.OrganizationUserRoles
                    .IgnoreQueryFilters()
                    .CountAsync(our => our.OrganizationId == currentUser.OrganizationId
                        && our.RoleId == ownerRole.Id,
                        cancellationToken);

                if (ownerCount <= 1)
                    return MemberErrors.CannotRemoveLastOwner;
            }
        }

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org?.ExternalId is not null && user.ExternalId is not null)
        {
            var clerkResult = await clerkService.RemoveMemberAsync(org.ExternalId, user.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
            {
                Log.Warning("Failed to remove user {UserId} from Clerk org — aborting deactivation", user.PublicId);
                return MemberErrors.SyncFailed;
            }
        }

        membership.Status = OrganizationUser.Statuses.Deactivated;

        var hasOtherActiveMemberships = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(ou => ou.UserId == user.Id
                && ou.OrganizationId != currentUser.OrganizationId
                && ou.Status == OrganizationUser.Statuses.Active,
                cancellationToken);

        if (!hasOtherActiveMemberships)
            user.IsActive = false;

        dbContext.AddOutboxEvent(new MemberDeactivatedEvent(user.Id, currentUser.OrganizationId));

        return Result.Success();
    }
}
```

- [ ] **Step 5: `ReactivateMemberCommand` — implement `ICacheInvalidating`**

```csharp
namespace Propentro.Application.Features.Organizations.Members.Commands.ReactivateMember;

public sealed record ReactivateMemberCommand(string UserId) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["user-permissions:*"];
}
```

- [ ] **Step 6: `ReactivateMemberHandler` — remove `IPermissionService` and manual cache call**

Remove `IPermissionService permissionService` from the primary constructor (line 13). Remove line 50 (`await permissionService.InvalidatePermissionsCacheAsync(...)`). Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The handler becomes:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.Commands.ReactivateMember;

public sealed class ReactivateMemberHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkService) : ICommandHandler<ReactivateMemberCommand>
{
    public async Task<Result> Handle(ReactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId, cancellationToken);

        if (user is null)
            return MemberErrors.NotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == currentUser.OrganizationId && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return MemberErrors.UserNotMember;

        if (membership.Status == OrganizationUser.Statuses.Active)
            return MemberErrors.AlreadyActive;

        membership.Status = OrganizationUser.Statuses.Active;
        user.IsActive = true;

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org?.ExternalId is not null && user.ExternalId is not null)
        {
            var clerkResult = await clerkService.AddMemberAsync(org.ExternalId, user.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                Log.Warning("Failed to re-add user {UserId} to Clerk org — member reactivated in DB but Clerk sync pending", user.PublicId);
        }

        dbContext.AddOutboxEvent(new MemberReactivatedEvent(user.Id, currentUser.OrganizationId));

        return Result.Success();
    }
}
```

- [ ] **Step 7: `UpdateMemberRolesCommand` — implement `ICacheInvalidating`**

```csharp
namespace Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;

public sealed record UpdateMemberRolesCommand(string UserId, string[] RoleIds) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["user-permissions:*"];
}
```

- [ ] **Step 8: `UpdateMemberRolesHandler` — remove `IPermissionService` and manual cache call**

Remove `IPermissionService permissionService` from the primary constructor (line 13). Remove line 97 (`await permissionService.InvalidatePermissionsCacheAsync(...)`). Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The handler becomes:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;

public sealed class UpdateMemberRolesHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateMemberRolesCommand>
{
    public async Task<Result> Handle(UpdateMemberRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId && u.IsActive, cancellationToken);

        if (user is null)
            return MemberErrors.NotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == currentUser.OrganizationId && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return MemberErrors.UserNotMember;

        if (membership.Status == OrganizationUser.Statuses.Deactivated)
            return MemberErrors.AlreadyDeactivated;

        var roles = await dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => request.RoleIds.Contains(r.PublicId)
                && r.IsActive
                && (r.IsSystem || r.OrganizationId == currentUser.OrganizationId))
            .ToListAsync(cancellationToken);

        if (roles.Count != request.RoleIds.Length)
            return MemberErrors.InvalidRoleIds;

        var ownerRole = roles.FirstOrDefault(r => r.Name == "Owner" && r.IsSystem);
        if (ownerRole is null)
        {
            var ownerSystemRole = await dbContext.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Name == "Owner" && r.IsSystem && r.IsActive, cancellationToken);

            if (ownerSystemRole is not null)
            {
                var ownerCount = await dbContext.OrganizationUserRoles
                    .IgnoreQueryFilters()
                    .CountAsync(
                        our => our.OrganizationId == currentUser.OrganizationId
                            && our.RoleId == ownerSystemRole.Id
                            && our.UserId != user.Id,
                        cancellationToken);

                var currentUserHasOwnerRole = await dbContext.OrganizationUserRoles
                    .IgnoreQueryFilters()
                    .AnyAsync(
                        our => our.OrganizationId == currentUser.OrganizationId
                            && our.UserId == user.Id
                            && our.RoleId == ownerSystemRole.Id,
                        cancellationToken);

                if (currentUserHasOwnerRole && ownerCount == 0)
                    return MemberErrors.CannotRemoveLastOwner;
            }
        }

        var existingRoles = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .Where(our => our.OrganizationId == currentUser.OrganizationId && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingRoles)
            dbContext.OrganizationUserRoles.Remove(existing);

        var now = dateTimeProvider.UtcNow;
        foreach (var role in roles)
        {
            dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
            {
                OrganizationId = currentUser.OrganizationId,
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = now,
                AssignedBy = currentUser.UserId
            });
        }

        dbContext.AddOutboxEvent(new MemberRolesUpdatedEvent(user.Id, currentUser.OrganizationId));

        return Result.Success();
    }
}
```

- [ ] **Step 9: Build**

Run: `dotnet build Propentro.slnx`
Expected: Build FAILS — admin handlers still reference removed `IPermissionService` methods. Expected; fixed in Task 5.

- [ ] **Step 10: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Settings/ src/Propentro.Application/Features/Organizations/Members/
git commit -m "feat: add ICacheInvalidating to settings and member commands, remove manual cache code"
```

---

### Task 5: Update admin handlers — replace IPermissionService with ICacheService

**Files:**
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs`
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/ReactivateOrganization/ReactivateOrganizationHandler.cs`
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/DeleteOrganization/DeleteOrganizationHandler.cs`
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/UpdateOrganization/UpdateOrganizationHandler.cs`
- Modify: `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/AdminDeactivateMemberHandler.cs`
- Modify: `src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/AdminReactivateMemberHandler.cs`
- Modify: `src/Propentro.Application/Features/Admin/Users/Commands/RemoveMember/RemoveMemberHandler.cs`
- Modify: `src/Propentro.Application/Features/Admin/Users/Commands/DeleteUser/DeleteUserHandler.cs`

- [ ] **Step 1: `DeactivateOrganizationHandler` — replace `IPermissionService` with `ICacheService`**

Replace `IPermissionService permissionService` with `ICacheService cache` in primary constructor. Replace line 95 (`await permissionService.InvalidateOrganizationPermissionsCacheAsync(org.Id, ...)`) with `await cache.RemoveByPatternAsync($"org:{org.Id}:*", cancellationToken)`. Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeactivateOrganization;

public sealed class DeactivateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<DeactivateOrganizationCommand>
{
    public async Task<Result> Handle(DeactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        if (!org.IsActive)
            return AdminOrganizationErrors.AlreadyInactive;

        if (org.ExternalId is not null)
        {
            var clerkResult = await clerkService.DeleteOrganizationAsync(org.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                return OrganizationErrors.SyncFailed;
        }

        var memberships = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .Where(ou => ou.OrganizationId == org.Id)
            .ToListAsync(cancellationToken);

        var memberUserIds = memberships.Select(ou => ou.UserId).ToList();

        var roles = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == org.Id)
            .ToListAsync(cancellationToken);

        dbContext.OrganizationUserRoles.RemoveRange(roles);
        dbContext.OrganizationUsers.RemoveRange(memberships);

        var pendingInvites = await dbContext.Invites
            .Where(i => i.OrganizationId == org.Id && i.Status == Invite.Statuses.Pending)
            .ToListAsync(cancellationToken);

        foreach (var invite in pendingInvites)
        {
            invite.Status = Invite.Statuses.Canceled;
        }

        org.IsActive = false;

        if (memberUserIds.Count > 0)
        {
            var usersWithOtherOrgs = await dbContext.OrganizationUsers
                .IgnoreQueryFilters()
                .Where(ou => memberUserIds.Contains(ou.UserId) && ou.OrganizationId != org.Id)
                .Select(ou => ou.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var orphanedUserIds = memberUserIds
                .Except(usersWithOtherOrgs)
                .ToList();

            if (orphanedUserIds.Count > 0)
            {
                var orphanedUsers = await dbContext.Users
                    .IgnoreQueryFilters()
                    .Where(u => orphanedUserIds.Contains(u.Id))
                    .ToListAsync(cancellationToken);

                foreach (var user in orphanedUsers)
                {
                    user.IsActive = false;
                }
            }
        }

        await cache.RemoveByPatternAsync($"org:{org.Id}:*", cancellationToken);

        Log.Information("Deactivated organization {PublicId} ({Name}) with {MemberCount} members",
            org.PublicId, org.Name, memberUserIds.Count);

        dbContext.AddOutboxEvent(new OrganizationDeactivatedEvent(org.Id, org.Id, memberUserIds));

        return Result.Success();
    }
}
```

- [ ] **Step 2: `ReactivateOrganizationHandler` — add `ICacheService` and org-settings invalidation**

Add `ICacheService cache` to the primary constructor. Add `await cache.RemoveAsync($"org:{org.Id}:org-settings", cancellationToken)` after reactivation. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.ReactivateOrganization;

public sealed class ReactivateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<ReactivateOrganizationCommand>
{
    public async Task<Result> Handle(
        ReactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        if (org.IsActive)
            return AdminOrganizationErrors.AlreadyActive;

        var clerkResult = await clerkService.CreateOrganizationAsync(
            org.Name, org.Slug, cancellationToken);

        if (clerkResult.IsFailure)
            return OrganizationErrors.SyncFailed;

        org.ExternalId = clerkResult.Value;
        org.IsActive = true;

        await cache.RemoveAsync($"org:{org.Id}:org-settings", cancellationToken);

        Log.Information("Reactivated organisation {PublicId} ({Name})", org.PublicId, org.Name);

        return Result.Success();
    }
}
```

- [ ] **Step 3: `DeleteOrganizationHandler` — replace `IPermissionService` with `ICacheService`**

Replace `IPermissionService permissionService` with `ICacheService cache` in primary constructor. Replace line 32-33 (`await permissionService.InvalidateOrganizationPermissionsCacheAsync(...)`) with `await cache.RemoveByPatternAsync($"org:{org.Id}:*", cancellationToken)`. Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<DeleteOrganizationCommand>
{
    public async Task<Result> Handle(
        DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        if (org.ExternalId is not null)
        {
            var clerkResult = await clerkService.DeleteOrganizationAsync(
                org.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                return OrganizationErrors.SyncFailed;
        }

        await cache.RemoveByPatternAsync($"org:{org.Id}:*", cancellationToken);

        dbContext.Organizations.Remove(org);

        Log.Information("Hard deleted organization {PublicId} ({Name})",
            org.PublicId, org.Name);

        return Result.Success();
    }
}
```

- [ ] **Step 4: `UpdateOrganizationHandler` — add `ICacheService` and org-settings invalidation**

Add `ICacheService cache` to the primary constructor. Add `await cache.RemoveAsync($"org:{org.Id}:org-settings", cancellationToken)` after field updates. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;

public sealed class UpdateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<UpdateOrganizationCommand, AdminOrganizationResponse>
{
    public async Task<Result<AdminOrganizationResponse>> Handle(
        UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null || !org.IsActive)
            return OrganizationErrors.NotFound;

        var nameChanged = org.Name != request.Name;

        org.Name = request.Name;
        org.ContactName = request.ContactName;
        org.ContactEmail = request.ContactEmail;
        org.ContactPhone = request.ContactPhone;
        org.CurrencyCode = request.CurrencyCode;
        org.Timezone = request.Timezone;

        if (nameChanged && org.ExternalId is not null)
        {
            var clerkResult = await clerkService.UpdateOrganizationAsync(
                org.ExternalId, request.Name, cancellationToken);

            if (clerkResult.IsFailure)
                return OrganizationErrors.SyncFailed;
        }

        await cache.RemoveAsync($"org:{org.Id}:org-settings", cancellationToken);

        var memberCount = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .CountAsync(ou => ou.OrganizationId == org.Id, cancellationToken);

        Log.Information("Updated organization {PublicId} ({Name})", org.PublicId, org.Name);

        return new AdminOrganizationResponse
        {
            OrganizationId = org.PublicId,
            Name = org.Name,
            Slug = org.Slug,
            ContactName = org.ContactName,
            ContactEmail = org.ContactEmail,
            ContactPhone = org.ContactPhone,
            CurrencyCode = org.CurrencyCode,
            Timezone = org.Timezone,
            SubscriptionPlan = org.SubscriptionPlan,
            IsActive = org.IsActive,
            MemberCount = memberCount,
            CreatedAt = org.CreatedAt
        };
    }
}
```

- [ ] **Step 5: `AdminDeactivateMemberHandler` — replace `IPermissionService` with `ICacheService`**

Replace `IPermissionService permissionService` with `ICacheService cache` in primary constructor. Replace line 53 (`await permissionService.InvalidatePermissionsCacheAsync(user.Id, org.Id, ...)`) with `await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken)`. Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

public sealed class AdminDeactivateMemberHandler(
    IAppDbContext dbContext,
    ICacheService cache) : ICommandHandler<AdminDeactivateMemberCommand>
{
    public async Task<Result> Handle(AdminDeactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.OrganizationId, cancellationToken);

        if (org is null)
            return AdminUserErrors.OrgNotFound;

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return AdminUserErrors.NotAMember;

        if (membership.Status == OrganizationUser.Statuses.Deactivated)
            return MemberErrors.AlreadyDeactivated;

        membership.Status = OrganizationUser.Statuses.Deactivated;

        var hasOtherActiveMemberships = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(ou => ou.UserId == user.Id
                && ou.OrganizationId != org.Id
                && ou.Status == OrganizationUser.Statuses.Active,
                cancellationToken);

        if (!hasOtherActiveMemberships)
            user.IsActive = false;

        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);

        Log.Information(
            "Admin deactivated member {UserPublicId} in organisation {OrgPublicId}",
            user.PublicId, org.PublicId);

        dbContext.AddOutboxEvent(new MemberDeactivatedEvent(user.Id, org.Id));

        return Result.Success();
    }
}
```

- [ ] **Step 6: `AdminReactivateMemberHandler` — replace `IPermissionService` with `ICacheService`**

Replace `IPermissionService permissionService` with `ICacheService cache` in primary constructor. Replace line 44 (`await permissionService.InvalidatePermissionsCacheAsync(user.Id, org.Id, ...)`) with `await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken)`. Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;

public sealed class AdminReactivateMemberHandler(
    IAppDbContext dbContext,
    ICacheService cache) : ICommandHandler<AdminReactivateMemberCommand>
{
    public async Task<Result> Handle(AdminReactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.OrganizationId, cancellationToken);

        if (org is null)
            return AdminUserErrors.OrgNotFound;

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return AdminUserErrors.NotAMember;

        if (membership.Status == OrganizationUser.Statuses.Active)
            return MemberErrors.AlreadyActive;

        membership.Status = OrganizationUser.Statuses.Active;
        user.IsActive = true;

        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);

        Log.Information(
            "Admin reactivated member {UserPublicId} in organisation {OrgPublicId}",
            user.PublicId, org.PublicId);

        dbContext.AddOutboxEvent(new MemberReactivatedEvent(user.Id, org.Id));

        return Result.Success();
    }
}
```

- [ ] **Step 7: `RemoveMemberHandler` — replace `IPermissionService` with `ICacheService`**

Replace `IPermissionService permissionService` with `ICacheService cache` in primary constructor. Replace line 72 (`await permissionService.InvalidatePermissionsCacheAsync(user.Id, org.Id, ...)`) with `await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken)`. Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Users.Commands.RemoveMember;

public sealed class RemoveMemberHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.OrganizationId, cancellationToken);

        if (org is null)
            return AdminUserErrors.OrgNotFound;

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return AdminUserErrors.NotAMember;

        var roles = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == org.Id && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        dbContext.OrganizationUserRoles.RemoveRange(roles);
        dbContext.OrganizationUsers.Remove(membership);

        if (org.ExternalId is not null && user.ExternalId is not null)
        {
            var clerkResult = await clerkService.RemoveMemberAsync(
                org.ExternalId, user.ExternalId, cancellationToken);

            if (clerkResult.IsFailure)
                return AdminUserErrors.SyncFailed;
        }

        var hasOtherOrgs = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                ou => ou.UserId == user.Id && ou.OrganizationId != org.Id,
                cancellationToken);

        if (!hasOtherOrgs)
        {
            user.IsActive = false;
        }

        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);

        Log.Information(
            "Removed user {UserPublicId} from organization {OrgPublicId}",
            user.PublicId, org.PublicId);

        dbContext.AddOutboxEvent(new MemberRemovedEvent(user.Id, org.Id));

        return Result.Success();
    }
}
```

- [ ] **Step 8: `DeleteUserHandler` — replace `IPermissionService` with `ICacheService`**

Replace `IPermissionService permissionService` with `ICacheService cache` in primary constructor. Replace the loop (lines 29-32) that calls `permissionService.InvalidatePermissionsCacheAsync` per org with a loop that calls `cache.RemoveByPatternAsync($"org:{orgId}:user-permissions:*", ...)`. Remove the `using` for `Propentro.Application.Common.Interfaces.Authorization`. The full handler:

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.OrganizationUsers)
            .FirstOrDefaultAsync(u => u.PublicId == request.Id, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        var organizationIds = user.OrganizationUsers
            .Select(ou => ou.OrganizationId)
            .ToList();

        foreach (var orgId in organizationIds)
        {
            await cache.RemoveByPatternAsync($"org:{orgId}:user-permissions:*", cancellationToken);
        }

        if (user.ExternalId is not null)
        {
            try
            {
                await clerkService.DeleteUserAsync(user.ExternalId, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete user {ExternalId} from Clerk during GDPR hard delete — proceeding with DB deletion", user.ExternalId);
            }
        }

        dbContext.Users.Remove(user);

        Log.Information("Hard deleted user {PublicId} ({Email}) — GDPR request", user.PublicId, user.Email);

        return Result.Success();
    }
}
```

- [ ] **Step 9: Build**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeds with 0 errors, 0 warnings.

- [ ] **Step 10: Run all tests**

Run: `dotnet test Propentro.slnx`
Expected: All tests pass.

- [ ] **Step 11: Commit**

```bash
git add src/Propentro.Application/Features/Admin/
git commit -m "refactor: replace IPermissionService cache calls with ICacheService in admin handlers"
```

---

### Task 6: Update CLAUDE.md with admin cache invalidation convention

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Add convention to CLAUDE.md**

Add the following under the `### Shared Services` section (after line ~170, in the Key Conventions area), as a new subsection:

```markdown
### Cache Invalidation
- **Org-scoped commands:** Implement `ICacheInvalidating` on the command record. `UnitOfWorkBehavior` handles post-commit invalidation with automatic `org:{OrgId}:` prefix. Wildcard keys (e.g., `"roles-*"`) use `RemoveByPatternAsync`.
- **Admin commands:** Use `ICacheService` directly in the handler with fully-qualified keys (e.g., `$"org:{org.Id}:user-permissions:*"`). Admin commands have no org context, so `ICacheInvalidating` cannot be used.
- **Permission cache key format:** `org:{orgId}:user-permissions:{userId}` — follows the `org:{OrgId}:` prefix convention.
```

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add cache invalidation convention to CLAUDE.md"
```

---

## Verification Checklist

After all tasks are complete:

- [ ] `dotnet build Propentro.slnx` — 0 errors, 0 warnings
- [ ] `dotnet test Propentro.slnx` — all tests pass
- [ ] No handler in `Features/Organizations/` imports `IPermissionService` or `ICacheService` for cache invalidation
- [ ] All 7 org-scoped commands implement `ICacheInvalidating`
- [ ] All 8 admin handlers use `ICacheService` directly with new key format
- [ ] `IPermissionService` has only `GetUserPermissionsAsync` and `HasPermissionAsync`
- [ ] `PermissionService.GetCacheKey` returns `org:{orgId}:user-permissions:{userId}`
- [ ] `UnitOfWorkBehavior.InvalidateCacheAsync` branches on `*` for pattern removal
