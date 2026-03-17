# API Production Hardening Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix runtime bugs, enforce American English, correct HTTP methods, add missing admin endpoints, and align the API with production-ready conventions defined in the design spec.

**Architecture:** CQRS via MediatR — commands mutate via EF Core, queries read via Dapper. Endpoints in minimal API groups. Result<T> pattern for error handling. Multi-tenant via IUserContext.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core, Dapper, MediatR, FluentValidation, xUnit, FluentAssertions, NSubstitute, Testcontainers

**Spec:** `docs/superpowers/specs/2026-03-16-api-production-hardening-design.md`

---

## Task 1: Fix Critical Runtime Bug — Invite Status "cancelled" vs "canceled"

The `Invite.Statuses.Cancelled` constant is `"cancelled"` but the DB CHECK constraint expects `"canceled"`. EF Core writes the wrong value, causing runtime failures.

**Files:**
- Modify: `src/Propentro.Domain/Entities/Identity/Invite.cs:13` — fix status constant
- Modify: `src/Propentro.Domain/Entities/Identity/Invite.cs:21,32` — rename `CancelledById` property and `CancelledByUser` nav
- Modify: `src/Propentro.Infrastructure/Persistence/Configurations/InviteConfiguration.cs:21,41-44` — fix EF mapping column name
- Modify: `src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/CancelInviteCommand.cs:5` — rename parameter
- Modify: `src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/CancelInviteHandler.cs:28-29,39` — fix property refs + log message
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs:58` — fix status ref

- [ ] **Step 1: Fix `Invite.cs` — rename `Cancelled` to `Canceled`, `CancelledById` to `CanceledById`, `CancelledByUser` to `CanceledByUser`**

In `src/Propentro.Domain/Entities/Identity/Invite.cs`:
```csharp
// Line 13: Change constant
public const string Cancelled = "cancelled";
// →
public const string Canceled = "canceled";

// Line 21: Rename property
public Guid? CancelledById { get; set; }
// →
public Guid? CanceledById { get; set; }

// Line 32: Rename navigation
public User? CancelledByUser { get; set; }
// →
public User? CanceledByUser { get; set; }
```

- [ ] **Step 2: Fix `InviteConfiguration.cs` — update EF mapping**

In `src/Propentro.Infrastructure/Persistence/Configurations/InviteConfiguration.cs`:
```csharp
// Line 21: Fix column mapping
builder.Property(e => e.CancelledById).HasColumnName("cancelled_by_id");
// →
builder.Property(e => e.CanceledById).HasColumnName("canceled_by_id");

// Lines 41-44: Fix navigation FK
builder.HasOne(e => e.CancelledByUser)
    .WithMany()
    .HasForeignKey(e => e.CancelledById)
// →
builder.HasOne(e => e.CanceledByUser)
    .WithMany()
    .HasForeignKey(e => e.CanceledById)
```

- [ ] **Step 3: Fix `CancelInviteCommand.cs` — rename parameter**

In `src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/CancelInviteCommand.cs`:
```csharp
public sealed record CancelInviteCommand(
    string Id,
    Guid CancelledByUserId,
// →
public sealed record CancelInviteCommand(
    string Id,
    Guid CanceledByUserId,
```

- [ ] **Step 4: Fix `CancelInviteHandler.cs` — update property references and log message**

In `src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/CancelInviteHandler.cs`:
```csharp
// Line 28:
invite.Status = Invite.Statuses.Cancelled;
// →
invite.Status = Invite.Statuses.Canceled;

// Line 29:
invite.CancelledById = request.CancelledByUserId;
// →
invite.CanceledById = request.CanceledByUserId;

// Line 39:
Log.Information("Cancelled invite {InvitePublicId} for {Email}",
// →
Log.Information("Canceled invite {InvitePublicId} for {Email}",
```

- [ ] **Step 5: Fix `DeactivateOrganizationHandler.cs` — update invite status reference**

In `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs`:
```csharp
// Line 58:
invite.Status = Invite.Statuses.Cancelled;
// →
invite.Status = Invite.Statuses.Canceled;
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build Propentro.slnx
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 7: Commit**

```bash
git add src/Propentro.Domain/Entities/Identity/Invite.cs \
  src/Propentro.Infrastructure/Persistence/Configurations/InviteConfiguration.cs \
  src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/CancelInviteCommand.cs \
  src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/CancelInviteHandler.cs \
  src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs
git commit -m "fix(domain): rename Cancelled to Canceled to match DB CHECK constraint

The Invite.Statuses.Cancelled constant was 'cancelled' but the DB CHECK
constraint expects 'canceled'. This caused runtime failures when EF Core
tried to write the wrong value. Also renames CancelledById → CanceledById
throughout the codebase."
```

---

## Task 2: Fix American English Violations — "organisation" → "organization"

23+ instances of British English in error messages, log messages, and inline error strings.

**Files:**
- Modify: `src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs` — 4 instances (lines 8, 14, 17, 26)
- Modify: `src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs` — 4 instances (lines 8, 11, 14, 17)
- Modify: `src/Propentro.Application/Features/Admin/Users/AdminUserErrors.cs` — 3 instances (lines 17, 20, 23)
- Modify: `src/Propentro.Application/Features/Admin/Invites/Commands/InviteUser/InviteUserHandler.cs` — 4 instances (lines 91, 116, 142, 165)
- Modify: `src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgHandler.cs` — 3 instances (lines 25, 120, 167)
- Modify: `src/Propentro.Application/Features/Admin/Users/Commands/RemoveMember/RemoveMemberHandler.cs` — 1 instance (line 74)
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/CreateOrganization/CreateOrganizationHandler.cs` — 1 instance (line 49)
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs` — 2 instances (lines 61, 95)
- Modify: `src/Propentro.Application/Features/Admin/Organizations/Commands/UpdateOrganization/UpdateOrganizationHandler.cs` — 1 instance (line 45)
- Modify: `src/Propentro.Api/Middleware/GlobalExceptionHandler.cs` — 4 instances (lines 81, 82, 84, 88)

- [ ] **Step 1: Fix `InviteErrors.cs` (Admin)**

In `src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs`:
```csharp
// Line 8: "Organisation not found." → "Organization not found."
// Line 14: "this organisation." → "this organization."
// Line 17: "email and organisation." → "email and organization."
// Line 26: "can be cancelled." → "can be canceled."
```

- [ ] **Step 2: Fix `AdminOrganizationErrors.cs`**

In `src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs`:
```csharp
// Line 8: "Organisation not found." → "Organization not found."
// Line 11: "An organisation with" → "An organization with"
// Line 14: "Clerk synchronisation" → "Clerk synchronization"
// Line 17: "Organisation is already" → "Organization is already"
```

- [ ] **Step 3: Fix `AdminUserErrors.cs`**

In `src/Propentro.Application/Features/Admin/Users/AdminUserErrors.cs`:
```csharp
// Line 17: "Organisation not found." → "Organization not found."
// Line 20: "this organisation." → "this organization."
// Line 23: "Clerk synchronisation" → "Clerk synchronization"
```

- [ ] **Step 4: Fix `InviteUserHandler.cs` (Admin) — 4 instances**

In `src/Propentro.Application/Features/Admin/Invites/Commands/InviteUser/InviteUserHandler.cs`:
```csharp
// Line 91: "Organisation is not linked" → "Organization is not linked"
// Line 116: "to organisation {OrgPublicId}" → "to organization {OrgPublicId}"
// Line 142: "Organisation is not linked" → "Organization is not linked"
// Line 165: "to organisation {OrgPublicId}" → "to organization {OrgPublicId}"
```

- [ ] **Step 5: Fix `InviteToCurrentOrgHandler.cs` — 3 instances**

In `src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgHandler.cs`:
```csharp
// Line 25: "Organisation is not linked" → "Organization is not linked"
// Line 120: "to organisation {OrgPublicId}" → "to organization {OrgPublicId}"
// Line 167: "to organisation {OrgPublicId}" → "to organization {OrgPublicId}"
```

- [ ] **Step 6: Fix remaining handlers**

In `src/Propentro.Application/Features/Admin/Users/Commands/RemoveMember/RemoveMemberHandler.cs`:
```csharp
// Line 74: "from organisation {OrgPublicId}" → "from organization {OrgPublicId}"
```

In `src/Propentro.Application/Features/Admin/Organizations/Commands/CreateOrganization/CreateOrganizationHandler.cs`:
```csharp
// Line 49: "Created organisation" → "Created organization"
```

In `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs`:
```csharp
// Line 61: comment "Deactivate the organisation" → "Deactivate the organization"
// Line 95: "Deactivated organisation" → "Deactivated organization"
```

In `src/Propentro.Application/Features/Admin/Organizations/Commands/UpdateOrganization/UpdateOrganizationHandler.cs`:
```csharp
// Line 45: "Updated organisation" → "Updated organization"
```

- [ ] **Step 7: Fix `GlobalExceptionHandler.cs` — user-facing "cancelled" text**

In `src/Propentro.Api/Middleware/GlobalExceptionHandler.cs`, fix the user-facing strings:
```csharp
// Line 81: "errors/cancelled" → "errors/canceled"
// Line 82: "Request Cancelled" → "Request Canceled"
// Line 84: "cancelled by the client" → "canceled by the client"
// Line 88: "REQUEST_CANCELLED" → "REQUEST_CANCELED"
```

Note: The C# method name `HandleCancelledException` references the .NET framework type `OperationCanceledException`, so leave the method name unchanged. Only fix the user-facing string values.

- [ ] **Step 8: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "fix(api): replace British English with American English across codebase

Fixes 23+ instances: organisation→organization, cancelled→canceled,
synchronisation→synchronization in error messages, log messages, inline
error strings, and exception handler."
```

---

## Task 3: Remove `isSystemAdmin` from `/v1/me` Response

Per design spec: admin status should come from Clerk `publicMetadata`, not from the `/me` endpoint.

**Files:**
- Modify: `src/Propentro.Contracts/Me/MeResponse.cs:11` — remove `IsSystemAdmin` parameter
- Modify: `src/Propentro.Application/Features/Me/Queries/GetMe/GetMeHandler.cs:67-88` — remove admin query and parameter

- [ ] **Step 1: Remove `IsSystemAdmin` from `MeResponse`**

In `src/Propentro.Contracts/Me/MeResponse.cs`:
```csharp
public sealed record MeResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    MeOrganizationResponse Organization,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
```

- [ ] **Step 2: Remove admin query from `GetMeHandler`**

In `src/Propentro.Application/Features/Me/Queries/GetMe/GetMeHandler.cs`:
- Remove lines 67-75 (the `adminSql` query and `isSystemAdmin` variable)
- Update `new MeResponse(...)` call on line 77-88 to remove last parameter

```csharp
var response = new MeResponse(
    result.UserId,
    result.Email,
    result.FirstName,
    result.LastName,
    new MeOrganizationResponse(
        result.OrganizationId,
        result.OrganizationName,
        result.OrganizationSlug),
    roles,
    permissions.Order().ToList());
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Contracts/Me/MeResponse.cs \
  src/Propentro.Application/Features/Me/Queries/GetMe/GetMeHandler.cs
git commit -m "feat(api): remove isSystemAdmin from /v1/me response

Admin status is determined via Clerk publicMetadata on the frontend.
The /me endpoint should only return org-scoped user profile data."
```

---

## Task 4: Fix HTTP Methods — POST → PATCH for State Transitions

Member deactivate/reactivate use POST but should use PATCH per conventions. Also standardize route params from `{userId}` to `{id}`.

**Files:**
- Modify: `src/Propentro.Api/Endpoints/Organizations/MembersEndpoints.cs:35,46,57,92-118` — change routes, methods, handler params

- [ ] **Step 1: Update `MembersEndpoints.cs` — change `MapPost` to `MapPatch`, `{userId}` to `{id}`**

In `src/Propentro.Api/Endpoints/Organizations/MembersEndpoints.cs`:

Change update member roles route (line 35):
```csharp
// FROM:
group.MapPatch("/{userId}", UpdateMemberRolesAsync)
// TO:
group.MapPatch("/{id}/roles", UpdateMemberRolesAsync)
```

Change deactivate route (line 46):
```csharp
// FROM:
group.MapPost("/{userId}/deactivate", DeactivateMemberAsync)
// TO:
group.MapPatch("/{id}/deactivate", DeactivateMemberAsync)
```

Change reactivate route (line 57):
```csharp
// FROM:
group.MapPost("/{userId}/reactivate", ReactivateMemberAsync)
// TO:
group.MapPatch("/{id}/reactivate", ReactivateMemberAsync)
```

Update the handler method signatures to use `string id` parameter instead of `string userId`:
```csharp
private static async Task<IResult> UpdateMemberRolesAsync(
    string id,
    UpdateMemberRolesRequest request,
    ISender sender,
    CancellationToken ct)
{
    var command = new UpdateMemberRolesCommand(id, request.RoleIds);
    ...
}

private static async Task<IResult> DeactivateMemberAsync(
    string id,
    ISender sender,
    CancellationToken ct)
{
    var result = await sender.Send(new DeactivateMemberCommand(id), ct);
    ...
}

private static async Task<IResult> ReactivateMemberAsync(
    string id,
    ISender sender,
    CancellationToken ct)
{
    var result = await sender.Send(new ReactivateMemberCommand(id), ct);
    ...
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Api/Endpoints/Organizations/MembersEndpoints.cs
git commit -m "fix(api): use PATCH for member state transitions, standardize route params

POST→PATCH for deactivate/reactivate (state transitions per conventions).
PATCH /{userId} → PATCH /{id}/roles (add /roles suffix).
Standardize path params to {id} per CLAUDE.md convention."
```

---

## Task 5: Extract Shared Invite Cancel Logic

The org-scoped cancel invite endpoint currently reuses `Admin.Invites.Commands.CancelInvite`. Per spec, extract the shared command to the Organizations feature area so admin reuses it.

**Files:**
- Create: `src/Propentro.Application/Features/Organizations/Invites/Commands/CancelInvite/CancelInviteCommand.cs`
- Create: `src/Propentro.Application/Features/Organizations/Invites/Commands/CancelInvite/CancelInviteHandler.cs`
- Modify: `src/Propentro.Api/Endpoints/Organizations/InviteEndpoints.cs:4` — update import
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs:4` — update import
- Remove: `src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/` (directory)

- [ ] **Step 1: Create org-scoped `CancelInviteCommand.cs`**

Create `src/Propentro.Application/Features/Organizations/Invites/Commands/CancelInvite/CancelInviteCommand.cs`:
```csharp
namespace Propentro.Application.Features.Organizations.Invites.Commands.CancelInvite;

public sealed record CancelInviteCommand(
    string Id,
    Guid CanceledByUserId,
    Guid? OrganizationId = null) : ICommand;
```

- [ ] **Step 2: Move handler to same directory**

Create `src/Propentro.Application/Features/Organizations/Invites/Commands/CancelInvite/CancelInviteHandler.cs` — copy from the admin version but update the namespace to `Organizations.Invites.Commands.CancelInvite`. Keep the import `using Propentro.Application.Features.Admin.Invites;` for `InviteErrors` (this follows the existing pattern — `InviteToCurrentOrgHandler` at line 4 already imports from `Admin.Invites`).

- [ ] **Step 3: Update imports in both endpoint files**

In `src/Propentro.Api/Endpoints/Organizations/InviteEndpoints.cs`, change:
```csharp
// FROM:
using Propentro.Application.Features.Admin.Invites.Commands.CancelInvite;
// TO:
using Propentro.Application.Features.Organizations.Invites.Commands.CancelInvite;
```

In `src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs`, change:
```csharp
// FROM:
using Propentro.Application.Features.Admin.Invites.Commands.CancelInvite;
// TO:
using Propentro.Application.Features.Organizations.Invites.Commands.CancelInvite;
```

- [ ] **Step 4: Delete old admin cancel invite directory**

Remove `src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/` (both files).

- [ ] **Step 5: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 6: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Invites/Commands/CancelInvite/ \
  src/Propentro.Api/Endpoints/Organizations/InviteEndpoints.cs \
  src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs
git rm -r src/Propentro.Application/Features/Admin/Invites/Commands/CancelInvite/
git commit -m "refactor(api): move CancelInviteCommand to Organizations feature area

Both org-scoped and admin cancel invite now use the same command from
Organizations.Invites.Commands.CancelInvite namespace."
```

---

## Task 6: Fix Admin Invite Cancel — DELETE → PATCH

Admin cancel invite uses `DELETE` but should use `PATCH .../cancel` for consistency with org-scoped endpoint. This task depends on Task 5 (shared command extraction) being complete.

**Files:**
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs:34` — change method and route

- [ ] **Step 1: Update `AdminInviteEndpoints.cs`**

In `src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs`:
```csharp
// Line 34: Change method and route
// FROM:
group.MapDelete("/invites/{id}", CancelAsync)
// TO:
group.MapPatch("/invites/{id}/cancel", CancelAsync)
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs
git commit -m "fix(api): change admin cancel invite from DELETE to PATCH /cancel

Aligns with org-scoped invite cancel and HTTP method conventions:
PATCH for state transitions, DELETE only for hard deletes."
```

---

## Task 7: Add Self-Deactivation Protection

Users must not be able to deactivate their own membership.

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Members/MemberErrors.cs` — add new error
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs` — add self-check

- [ ] **Step 1: Add `CannotDeactivateSelf` error to `MemberErrors.cs`**

In `src/Propentro.Application/Features/Organizations/Members/MemberErrors.cs`, add after `AlreadyActive`:
```csharp
public static readonly Error CannotDeactivateSelf = new(
    "Member.CannotDeactivateSelf",
    "You cannot deactivate your own membership.",
    ErrorType.Forbidden);
```

- [ ] **Step 2: Add self-deactivation check in `DeactivateMemberHandler.cs`**

In `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs`, after the `AlreadyDeactivated` check (line 33) and before the last-owner guard (line 36):
```csharp
// Block self-deactivation
if (user.Id == currentUser.UserId)
    return MemberErrors.CannotDeactivateSelf;
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Members/MemberErrors.cs \
  src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs
git commit -m "feat(api): block self-deactivation of membership

Users cannot deactivate their own membership. Returns 403 Forbidden
with error code Member.CannotDeactivateSelf."
```

---

## Task 8: Fix `Invite.IsExpired` to Use `IDateTimeProvider`

The `IsExpired` computed property uses `DateTimeOffset.UtcNow` directly, violating the convention.

**Files:**
- Modify: `src/Propentro.Domain/Entities/Identity/Invite.cs:34-35` — change computed properties to accept parameter

- [ ] **Step 1: Find all usages of `IsExpired` and `IsPendingAndValid`**

```bash
dotnet build Propentro.slnx 2>&1 | head -50
```
After modifying, the build will show all call sites. But first, search:
```bash
grep -rn "IsExpired\|IsPendingAndValid" src/
```

- [ ] **Step 2: Replace computed properties with methods that accept `DateTimeOffset`**

In `src/Propentro.Domain/Entities/Identity/Invite.cs`:
```csharp
// Remove these (lines 34-35):
public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;
public bool IsPendingAndValid => Status == Statuses.Pending && !IsExpired;

// Replace with:
public bool IsExpired(DateTimeOffset utcNow) => ExpiresAt.HasValue && ExpiresAt.Value < utcNow;
public bool IsPendingAndValid(DateTimeOffset utcNow) => Status == Statuses.Pending && !IsExpired(utcNow);
```

- [ ] **Step 3: Update all callers to pass `dateTimeProvider.UtcNow`**

Find and update every call site from step 1 grep results. Each caller should already have `IDateTimeProvider` injected (or add it if missing).

- [ ] **Step 4: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 5: Commit**

```bash
git add src/Propentro.Domain/Entities/Identity/Invite.cs
# + any modified handler files
git commit -m "fix(domain): use IDateTimeProvider for invite expiry checks

Replace DateTimeOffset.UtcNow in Invite.IsExpired with a parameter,
so callers pass dateTimeProvider.UtcNow. Makes expiry testable."
```

---

## Task 9: Convert Roles List to `PagedResult<RoleResponse>`

The `GET /v1/organizations/roles` endpoint returns `IReadOnlyList<RoleResponse>` instead of `PagedResult<RoleResponse>`.

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Queries/GetOrganizationRoles/GetOrganizationRolesQuery.cs` — add pagination params, change return type
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Queries/GetOrganizationRoles/GetOrganizationRolesHandler.cs` — add pagination to SQL
- Modify: `src/Propentro.Api/Endpoints/Organizations/RolesEndpoints.cs` — pass pagination query params

- [ ] **Step 1: Update `GetOrganizationRolesQuery.cs`**

```csharp
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Application.Features.Organizations.Roles.Queries.GetOrganizationRoles;

public sealed record GetOrganizationRolesQuery(
    int Page = 1,
    int PageSize = 25) : ICachedQuery<PagedResult<RoleResponse>>
{
    public string CacheKey => $"roles-{Page}-{PageSize}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

- [ ] **Step 2: Update `GetOrganizationRolesHandler.cs` — add COUNT query and LIMIT/OFFSET**

Change handler signature from `IQueryHandler<GetOrganizationRolesQuery, IReadOnlyList<RoleResponse>>` to `IQueryHandler<GetOrganizationRolesQuery, PagedResult<RoleResponse>>`.

Add a COUNT query before the roles query:
```sql
SELECT COUNT(*)
FROM roles r
WHERE (r.organization_id = @OrganizationId OR r.is_system = true)
  AND r.is_active = true
```

Add `LIMIT @PageSize OFFSET @Offset` to the existing roles SQL. Calculate `Offset = (request.Page - 1) * request.PageSize`.

Construct and return:
```csharp
return new PagedResult<RoleResponse>(result, totalCount, request.Page, request.PageSize);
```

Reference: Check existing paginated handlers (e.g., `GetOrganizationMembersHandler`) for the exact `PagedResult<T>` constructor pattern.

- [ ] **Step 3: Update `RolesEndpoints.cs` — pass pagination params**

Add `int page = 1, int pageSize = 25` parameters to the list endpoint handler and pass to `GetOrganizationRolesQuery(page, pageSize)`.

- [ ] **Step 4: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 5: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Roles/Queries/GetOrganizationRoles/ \
  src/Propentro.Api/Endpoints/Organizations/RolesEndpoints.cs
git commit -m "feat(api): paginate roles list endpoint with PagedResult<RoleResponse>

All list endpoints must return PagedResult<T> per CLAUDE.md convention.
Adds page/pageSize params and COUNT query."
```

---

## Task 10: Implement Role Deletion with Viewer Fallback

The current `DeleteRoleHandler` blocks deletion when members are assigned. Per spec, it should instead assign Viewer role to affected members, then delete.

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Commands/DeleteRole/DeleteRoleHandler.cs` — replace "block if members" with "assign Viewer then delete"
- Modify: `src/Propentro.Application/Features/Organizations/Roles/RoleErrors.cs` — remove `HasActiveMembers` error (no longer needed)

- [ ] **Step 1: Rewrite `DeleteRoleHandler.cs` role deletion logic**

In `src/Propentro.Application/Features/Organizations/Roles/Commands/DeleteRole/DeleteRoleHandler.cs`, replace lines 26-34 (the "check no active members" block) with:

```csharp
// Find all members with this role
var memberRoleAssignments = await dbContext.OrganizationUserRoles
    .IgnoreQueryFilters()
    .Where(our => our.OrganizationId == currentUser.OrganizationId && our.RoleId == role.Id)
    .ToListAsync(cancellationToken);

if (memberRoleAssignments.Count > 0)
{
    // Find the Viewer system role
    var viewerRole = await dbContext.Roles
        .IgnoreQueryFilters()
        .FirstAsync(r => r.Name == "Viewer" && r.IsSystem && r.IsActive, cancellationToken);

    // For each member: if they don't already have Viewer, assign it
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

    // Remove all assignments of the role being deleted
    foreach (var assignment in memberRoleAssignments)
        dbContext.OrganizationUserRoles.Remove(assignment);
}
```

Note: The handler will need `IDateTimeProvider` injected. Add it to the constructor parameters if not already present.

- [ ] **Step 2: Remove `HasActiveMembers` error from `RoleErrors.cs`**

In `src/Propentro.Application/Features/Organizations/Roles/RoleErrors.cs`, remove:
```csharp
public static readonly Error HasActiveMembers = new(
    "Role.HasActiveMembers",
    "Cannot delete a role that has active members assigned to it.",
    ErrorType.Conflict);
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build Propentro.slnx
```
If `HasActiveMembers` is referenced elsewhere, the build will catch it.

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Roles/Commands/DeleteRole/DeleteRoleHandler.cs \
  src/Propentro.Application/Features/Organizations/Roles/RoleErrors.cs
git commit -m "feat(api): implement role deletion with Viewer fallback

When deleting a custom role, members with that role get Viewer assigned
(if not already Viewer), then the role is deleted. Replaces the previous
behavior that blocked deletion when members were assigned."
```

---

## Task 11: Add `GET /v1/admin/me` Endpoint

New endpoint for super admin profile.

**Files:**
- Create: `src/Propentro.Contracts/Admin/Me/AdminMeResponse.cs`
- Create: `src/Propentro.Application/Features/Admin/Me/Queries/GetAdminMe/GetAdminMeQuery.cs`
- Create: `src/Propentro.Application/Features/Admin/Me/Queries/GetAdminMe/GetAdminMeHandler.cs`
- Create: `src/Propentro.Api/Endpoints/Admin/AdminMeEndpoints.cs`
- Modify: `src/Propentro.Api/Endpoints/EndpointExtensions.cs` — register new endpoint

- [ ] **Step 1: Create `AdminMeResponse.cs`**

Create `src/Propentro.Contracts/Admin/Me/AdminMeResponse.cs`:
```csharp
namespace Propentro.Contracts.Admin.Me;

public sealed record AdminMeResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string AdminRole { get; init; } = null!;
    public DateTimeOffset AssignedAt { get; init; }
}
```

- [ ] **Step 2: Create `GetAdminMeQuery.cs`**

Create `src/Propentro.Application/Features/Admin/Me/Queries/GetAdminMe/GetAdminMeQuery.cs`:
```csharp
using Propentro.Contracts.Admin.Me;

namespace Propentro.Application.Features.Admin.Me.Queries.GetAdminMe;

public sealed record GetAdminMeQuery : IQuery<AdminMeResponse>;
```

- [ ] **Step 3: Create `GetAdminMeHandler.cs`**

Create `src/Propentro.Application/Features/Admin/Me/Queries/GetAdminMe/GetAdminMeHandler.cs`:
```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Me;

namespace Propentro.Application.Features.Admin.Me.Queries.GetAdminMe;

public sealed class GetAdminMeHandler(
    IDbConnectionFactory db,
    IUserContext userContext) : IQueryHandler<GetAdminMeQuery, AdminMeResponse>
{
    public async Task<Result<AdminMeResponse>> Handle(
        GetAdminMeQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                u.public_id AS user_id,
                u.email,
                u.first_name,
                u.last_name,
                sa.role AS admin_role,
                sa.created_at AS assigned_at
            FROM system_administrators sa
            JOIN users u ON u.id = sa.user_id
            WHERE sa.user_id = @UserId
              AND sa.is_active = true
              AND u.is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<AdminMeResponse>(
            sql, new { userContext.UserId });

        if (result is null)
            return Error.NotFound("Admin.NotFound", "Admin profile not found.");

        return result;
    }
}
```

- [ ] **Step 4: Create `AdminMeEndpoints.cs`**

Create `src/Propentro.Api/Endpoints/Admin/AdminMeEndpoints.cs`:
```csharp
using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Me.Queries.GetAdminMe;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminMeEndpoints
{
    public static void MapAdminMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin")
            .WithTags("Admin Profile")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/me", GetAdminMeAsync)
            .WithName("AdminGetMe")
            .WithSummary("Get admin profile");
    }

    private static async Task<IResult> GetAdminMeAsync(
        ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminMeQuery(), ct);
        return result.ToHttpResult();
    }
}
```

- [ ] **Step 5: Register in `EndpointExtensions.cs`**

In `src/Propentro.Api/Endpoints/EndpointExtensions.cs`, add in the Admin section (after line 22):
```csharp
app.MapAdminMeEndpoints();
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 7: Commit**

```bash
git add src/Propentro.Contracts/Admin/Me/ \
  src/Propentro.Application/Features/Admin/Me/ \
  src/Propentro.Api/Endpoints/Admin/AdminMeEndpoints.cs \
  src/Propentro.Api/Endpoints/EndpointExtensions.cs
git commit -m "feat(api): add GET /v1/admin/me endpoint for admin profile

Returns admin user profile with role and assignment date.
Requires RequireSystemAdministrator authorization."
```

---

## Task 12: Add `PATCH /v1/admin/organizations/{id}/reactivate` Endpoint

New endpoint to reactivate a deactivated organization.

**Files:**
- Modify: `src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs` — add `AlreadyActive` error
- Create: `src/Propentro.Application/Features/Admin/Organizations/Commands/ReactivateOrganization/ReactivateOrganizationCommand.cs`
- Create: `src/Propentro.Application/Features/Admin/Organizations/Commands/ReactivateOrganization/ReactivateOrganizationHandler.cs`
- Create: `src/Propentro.Application/Features/Admin/Organizations/Commands/ReactivateOrganization/ReactivateOrganizationValidator.cs`
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminOrganizationEndpoints.cs` — add reactivate route

- [ ] **Step 1: Add `AlreadyActive` error**

In `src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs`, add:
```csharp
public static readonly Error AlreadyActive = Error.Conflict(
    "Organization.AlreadyActive", "Organization is already active.");
```

- [ ] **Step 2: Create `ReactivateOrganizationCommand.cs`**

```csharp
namespace Propentro.Application.Features.Admin.Organizations.Commands.ReactivateOrganization;

public sealed record ReactivateOrganizationCommand(string Id) : ICommand;
```

- [ ] **Step 3: Create `ReactivateOrganizationValidator.cs`**

Follow existing pattern from `DeactivateOrganizationValidator` — validate `Id` format with `org_` prefix regex.

- [ ] **Step 4: Create `ReactivateOrganizationHandler.cs`**

Prerequisite: Check `IClerkOrganizationService` interface for available methods. The deactivate handler (`DeactivateOrganizationHandler`) calls `clerkService.DeleteOrganizationAsync()` — meaning the Clerk org is deleted on deactivation. So reactivation needs to re-create the Clerk org if applicable.

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.ReactivateOrganization;

public sealed class ReactivateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService) : ICommandHandler<ReactivateOrganizationCommand>
{
    public async Task<Result> Handle(
        ReactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return AdminOrganizationErrors.NotFound;

        if (org.IsActive)
            return AdminOrganizationErrors.AlreadyActive;

        // Re-create org in Clerk (deactivation deletes the Clerk org)
        if (org.ExternalId is not null)
        {
            var clerkResult = await clerkService.CreateOrganizationAsync(
                org.Name, org.Slug, cancellationToken);
            if (clerkResult.IsFailure)
                return AdminOrganizationErrors.ClerkSyncFailed;
        }

        org.IsActive = true;
        // Members stay deactivated — must be individually reactivated

        Log.Information("Reactivated organization {PublicId} ({Name})",
            org.PublicId, org.Name);

        return Result.Success();
    }
}
```

Note: Adapt the Clerk reactivation call based on what `IClerkOrganizationService.CreateOrganizationAsync` actually returns and whether it returns the new external ID that needs to be updated on the org entity.

- [ ] **Step 5: Register endpoint in `AdminOrganizationEndpoints.cs`**

Add after the deactivate mapping:
```csharp
group.MapPatch("/{id}/reactivate", ReactivateAsync)
    .WithName("AdminReactivateOrganization")
    .WithSummary("Reactivate organization");
```

Add handler method:
```csharp
private static async Task<IResult> ReactivateAsync(
    string id, ISender sender, CancellationToken ct)
{
    var result = await sender.Send(new ReactivateOrganizationCommand(id), ct);
    return result.ToHttpResult();
}
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 7: Commit**

```bash
git add src/Propentro.Application/Features/Admin/Organizations/ \
  src/Propentro.Api/Endpoints/Admin/AdminOrganizationEndpoints.cs
git commit -m "feat(api): add PATCH /v1/admin/organizations/{id}/reactivate

Reactivates a deactivated organization. Members stay deactivated
and must be individually reactivated."
```

---

## Task 13: Add `DELETE /v1/admin/organizations/{id}` Endpoint

Hard delete organization with CASCADE. Requires `X-Confirm-Delete` header.

**Files:**
- Create: `src/Propentro.Application/Features/Admin/Organizations/Commands/DeleteOrganization/DeleteOrganizationCommand.cs`
- Create: `src/Propentro.Application/Features/Admin/Organizations/Commands/DeleteOrganization/DeleteOrganizationHandler.cs`
- Create: `src/Propentro.Application/Features/Admin/Organizations/Commands/DeleteOrganization/DeleteOrganizationValidator.cs`
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminOrganizationEndpoints.cs` — add DELETE route with header check

- [ ] **Step 1: Create `DeleteOrganizationCommand.cs`**

```csharp
namespace Propentro.Application.Features.Admin.Organizations.Commands.DeleteOrganization;

public sealed record DeleteOrganizationCommand(string Id) : ICommand;
```

- [ ] **Step 2: Create `DeleteOrganizationValidator.cs`**

Validate `Id` format with `org_` prefix regex (same pattern as other admin org validators).

- [ ] **Step 3: Create `DeleteOrganizationHandler.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<DeleteOrganizationCommand>
{
    public async Task<Result> Handle(
        DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return AdminOrganizationErrors.NotFound;

        // Delete from Clerk first
        if (org.ExternalId is not null)
        {
            var clerkResult = await clerkService.DeleteOrganizationAsync(
                org.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                return AdminOrganizationErrors.ClerkSyncFailed;
        }

        // Invalidate permission cache before deleting
        await permissionService.InvalidateOrganizationPermissionsCacheAsync(
            org.Id, cancellationToken);

        // Hard delete — CASCADE handles members, roles, invites
        dbContext.Organizations.Remove(org);

        Log.Information("Hard deleted organization {PublicId} ({Name})",
            org.PublicId, org.Name);

        return Result.Success();
    }
}
```

- [ ] **Step 4: Add endpoint with `X-Confirm-Delete` header validation**

In `AdminOrganizationEndpoints.cs`, add:
```csharp
group.MapDelete("/{id}", DeleteAsync)
    .WithName("AdminDeleteOrganization")
    .WithSummary("Hard delete organization (irreversible)");
```

Handler method:
```csharp
private static async Task<IResult> DeleteAsync(
    string id, HttpContext httpContext, ISender sender, CancellationToken ct)
{
    var confirmHeader = httpContext.Request.Headers["X-Confirm-Delete"].FirstOrDefault();
    if (string.IsNullOrEmpty(confirmHeader) || confirmHeader != id)
    {
        return Results.Problem(
            statusCode: 400,
            title: "Confirmation Required",
            detail: "X-Confirm-Delete header must match the organization ID.");
    }

    var result = await sender.Send(new DeleteOrganizationCommand(id), ct);
    return result.ToHttpResult();
}
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 6: Commit**

```bash
git add src/Propentro.Application/Features/Admin/Organizations/Commands/DeleteOrganization/ \
  src/Propentro.Api/Endpoints/Admin/AdminOrganizationEndpoints.cs
git commit -m "feat(api): add DELETE /v1/admin/organizations/{id} for hard delete

Irreversible CASCADE deletion of organization and all related data.
Requires X-Confirm-Delete header matching the organization public ID."
```

---

## Task 14: Add Admin Member Deactivate/Reactivate Endpoints

New endpoints: `PATCH /v1/admin/organizations/{organizationId}/members/{id}/deactivate` and `/reactivate`.

**Files:**
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/AdminDeactivateMemberCommand.cs`
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/AdminDeactivateMemberHandler.cs`
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/AdminDeactivateMemberValidator.cs`
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/AdminReactivateMemberCommand.cs`
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/AdminReactivateMemberHandler.cs`
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/AdminReactivateMemberValidator.cs`
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminMemberEndpoints.cs` — add routes + rename `{userId}` to `{id}`

- [ ] **Step 1: Create `AdminDeactivateMemberCommand.cs`**

```csharp
namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

public sealed record AdminDeactivateMemberCommand(
    string OrganizationId,
    string UserId) : ICommand;
```

- [ ] **Step 2: Create `AdminDeactivateMemberValidator.cs`**

Validate both `OrganizationId` (org_ prefix regex) and `UserId` (usr_ prefix regex). Follow existing validator pattern.

- [ ] **Step 3: Create `AdminDeactivateMemberHandler.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members;

namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

public sealed class AdminDeactivateMemberHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<AdminDeactivateMemberCommand>
{
    public async Task<Result> Handle(
        AdminDeactivateMemberCommand request, CancellationToken cancellationToken)
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
            return MemberErrors.NotFound;

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

        await permissionService.InvalidatePermissionsCacheAsync(
            user.Id, org.Id, cancellationToken);

        Log.Information("Admin deactivated member {UserId} in organization {OrgId}",
            user.PublicId, org.PublicId);

        return Result.Success();
    }
}
```

- [ ] **Step 4: Create `AdminReactivateMemberCommand.cs`**

```csharp
namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;

public sealed record AdminReactivateMemberCommand(
    string OrganizationId,
    string UserId) : ICommand;
```

- [ ] **Step 5: Create `AdminReactivateMemberValidator.cs` and `AdminReactivateMemberHandler.cs`**

Validator: same pattern as deactivate. Handler: mirror of deactivate — verify deactivated → set active, invalidate cache.

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members;

namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;

public sealed class AdminReactivateMemberHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<AdminReactivateMemberCommand>
{
    public async Task<Result> Handle(
        AdminReactivateMemberCommand request, CancellationToken cancellationToken)
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
            return MemberErrors.NotFound;

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

        await permissionService.InvalidatePermissionsCacheAsync(
            user.Id, org.Id, cancellationToken);

        Log.Information("Admin reactivated member {UserId} in organization {OrgId}",
            user.PublicId, org.PublicId);

        return Result.Success();
    }
}
```

- [ ] **Step 6: Update `AdminMemberEndpoints.cs` — add routes + rename `{userId}` to `{id}`**

In `src/Propentro.Api/Endpoints/Admin/AdminMemberEndpoints.cs`:

First, rename existing `{userId}` to `{id}` for consistency:
```csharp
// FROM:
group.MapDelete("/{userId}", RemoveAsync)
// TO:
group.MapDelete("/{id}", RemoveAsync)
```

Update `RemoveAsync` method signature from `string userId` to `string id`:
```csharp
private static async Task<IResult> RemoveAsync(
    string organizationId, string id,
    ISender sender, CancellationToken cancellationToken)
{
    var result = await sender.Send(
        new RemoveMemberCommand(organizationId, id), cancellationToken);
    return result.ToHttpResult();
}
```

Then add the new endpoints:
```csharp
group.MapPatch("/{id}/deactivate", DeactivateAsync)
    .WithName("AdminDeactivateMember")
    .WithSummary("Deactivate member in organization");

group.MapPatch("/{id}/reactivate", ReactivateAsync)
    .WithName("AdminReactivateMember")
    .WithSummary("Reactivate member in organization");
```

Handler methods:
```csharp
private static async Task<IResult> DeactivateAsync(
    string organizationId, string id,
    ISender sender, CancellationToken cancellationToken)
{
    var result = await sender.Send(
        new AdminDeactivateMemberCommand(organizationId, id), cancellationToken);
    return result.ToHttpResult();
}

private static async Task<IResult> ReactivateAsync(
    string organizationId, string id,
    ISender sender, CancellationToken cancellationToken)
{
    var result = await sender.Send(
        new AdminReactivateMemberCommand(organizationId, id), cancellationToken);
    return result.ToHttpResult();
}
```

- [ ] **Step 7: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 8: Commit**

```bash
git add src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/ \
  src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/ \
  src/Propentro.Api/Endpoints/Admin/AdminMemberEndpoints.cs
git commit -m "feat(api): add admin member deactivate/reactivate endpoints

PATCH /v1/admin/organizations/{organizationId}/members/{id}/deactivate
PATCH /v1/admin/organizations/{organizationId}/members/{id}/reactivate
Also standardizes {userId} to {id} on existing DELETE route."
```

---

## Task 15: Add `DELETE /v1/admin/users/{id}` Endpoint (GDPR Hard Delete)

Hard delete user with SET NULL on FKs. Requires `X-Confirm-Delete` header.

**Files:**
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/DeleteUser/DeleteUserCommand.cs`
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/DeleteUser/DeleteUserHandler.cs`
- Create: `src/Propentro.Application/Features/Admin/Users/Commands/DeleteUser/DeleteUserValidator.cs`
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminUserEndpoints.cs` — add DELETE route

- [ ] **Step 1: Create `DeleteUserCommand.cs`**

```csharp
namespace Propentro.Application.Features.Admin.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(string Id) : ICommand;
```

- [ ] **Step 2: Create `DeleteUserValidator.cs`**

Validate `Id` format with `usr_` prefix regex.

- [ ] **Step 3: Create `DeleteUserHandler.cs`**

Note: Check if `IClerkOrganizationService` has a `DeleteUserAsync` method. If not, the interface and implementation need extending. Alternatively, there may be a separate `IClerkUserService`.

```csharp
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(
        DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.Id, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        // Delete from Clerk — GDPR takes priority, so log failure but continue
        if (user.ExternalId is not null)
        {
            try
            {
                await clerkService.DeleteUserAsync(user.ExternalId, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed to delete user {UserId} from Clerk — proceeding with DB deletion for GDPR compliance",
                    user.PublicId);
            }
        }

        // Invalidate permission caches for all orgs
        var orgIds = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .Where(ou => ou.UserId == user.Id)
            .Select(ou => ou.OrganizationId)
            .ToListAsync(cancellationToken);

        foreach (var orgId in orgIds)
        {
            await permissionService.InvalidatePermissionsCacheAsync(
                user.Id, orgId, cancellationToken);
        }

        // Hard delete — FK SET NULL handles audit attribution
        dbContext.Users.Remove(user);

        Log.Information("Hard deleted user {PublicId} ({Email}) for GDPR compliance",
            user.PublicId, user.Email);

        return Result.Success();
    }
}
```

- [ ] **Step 4: Register endpoint with header validation**

In `AdminUserEndpoints.cs`, add:
```csharp
group.MapDelete("/{id}", DeleteAsync)
    .WithName("AdminDeleteUser")
    .WithSummary("Hard delete user (GDPR, irreversible)");
```

Handler method:
```csharp
private static async Task<IResult> DeleteAsync(
    string id, HttpContext httpContext, ISender sender, CancellationToken cancellationToken)
{
    var confirmHeader = httpContext.Request.Headers["X-Confirm-Delete"].FirstOrDefault();
    if (string.IsNullOrEmpty(confirmHeader) || confirmHeader != id)
    {
        return Results.Problem(
            statusCode: 400,
            title: "Confirmation Required",
            detail: "X-Confirm-Delete header must match the user ID.");
    }

    var result = await sender.Send(new DeleteUserCommand(id), cancellationToken);
    return result.ToHttpResult();
}
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 6: Commit**

```bash
git add src/Propentro.Application/Features/Admin/Users/Commands/DeleteUser/ \
  src/Propentro.Api/Endpoints/Admin/AdminUserEndpoints.cs
git commit -m "feat(api): add DELETE /v1/admin/users/{id} for GDPR hard delete

Irreversible user deletion. FKs SET NULL preserves referential integrity
but loses audit attribution. Clerk failure logged but not blocking
(GDPR compliance takes priority). Requires X-Confirm-Delete header."
```

---

## Task 16: Differentiate Invite Response — 200 vs 201

Existing user → 200 (direct add). New user → 201 (pending invite).

**Files:**
- Create: `src/Propentro.Contracts/Organizations/Invites/InviteResult.cs`
- Modify: `src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgCommand.cs` — change to `ICommand<InviteResult>`
- Modify: `src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgHandler.cs` — return typed result
- Modify: `src/Propentro.Application/Features/Admin/Invites/Commands/InviteUser/InviteUserCommand.cs` — change to `ICommand<InviteResult>`
- Modify: `src/Propentro.Application/Features/Admin/Invites/Commands/InviteUser/InviteUserHandler.cs` — return typed result
- Modify: `src/Propentro.Api/Endpoints/Organizations/InviteEndpoints.cs` — return 200 or 201 based on result
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs` — same

- [ ] **Step 1: Create `InviteResult` response type**

Create `src/Propentro.Contracts/Organizations/Invites/InviteResult.cs`:
```csharp
namespace Propentro.Contracts.Organizations.Invites;

public sealed record InviteResult(
    bool IsDirectAdd,
    string? MemberPublicId = null,
    string? InvitePublicId = null);
```

- [ ] **Step 2: Update org-scoped command to `ICommand<InviteResult>`**

In `src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgCommand.cs`:
```csharp
// FROM:
public sealed record InviteToCurrentOrgCommand(...) : ICommand;
// TO:
public sealed record InviteToCurrentOrgCommand(...) : ICommand<InviteResult>;
```

Update handler: change `ICommandHandler<InviteToCurrentOrgCommand>` to `ICommandHandler<InviteToCurrentOrgCommand, InviteResult>`, return type `Task<Result<InviteResult>>`.

In `HandleExistingUserAsync` return `new InviteResult(true, user.PublicId)`.
In `HandleNewUserAsync` return `new InviteResult(false, InvitePublicId: invitePublicId)` (capture the generated public ID).

- [ ] **Step 3: Update admin command similarly**

Same changes for `InviteUserCommand` and `InviteUserHandler`.

- [ ] **Step 4: Update endpoint handlers to return 200 or 201**

In both `InviteEndpoints.cs` and `AdminInviteEndpoints.cs`:
```csharp
return result.Match(
    inviteResult => inviteResult.IsDirectAdd
        ? Results.Ok(inviteResult)
        : Results.Created($"/invites/{inviteResult.InvitePublicId}", inviteResult),
    error => error.ToHttpResult());
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build Propentro.slnx
```

- [ ] **Step 6: Commit**

```bash
git add src/Propentro.Contracts/Organizations/Invites/ \
  src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/ \
  src/Propentro.Application/Features/Admin/Invites/Commands/InviteUser/ \
  src/Propentro.Api/Endpoints/Organizations/InviteEndpoints.cs \
  src/Propentro.Api/Endpoints/Admin/AdminInviteEndpoints.cs
git commit -m "feat(api): return 200 for direct add, 201 for pending invite

Differentiate HTTP response based on invite flow: existing user added
directly returns 200, new user with pending invite returns 201."
```

---

## Task 17: Update CLAUDE.md with New Conventions

**Files:**
- Modify: `CLAUDE.md` — add lifecycle rules, invite flow, HTTP method clarification

- [ ] **Step 1: Add entity lifecycle rules section**

Add after "## Key Conventions" in `CLAUDE.md`:

```markdown
### Entity Lifecycle Rules
- **Organization**: Deactivate (`is_active = false`) or CASCADE hard delete (super admin only)
- **User**: Deactivate (`is_active = false`) or hard delete with SET NULL FKs (GDPR, super admin only)
- **Membership**: Deactivate (`status = 'deactivated'`, temporary) or hard delete (`organization_users` row deleted, permanent)
- Org deactivate → all members deactivated. Org reactivate → org active, members stay deactivated.
- Self-deactivation prohibited (`Member.CannotDeactivateSelf`)
- Hard deletes require `X-Confirm-Delete` header matching the resource public ID
```

- [ ] **Step 2: Add invite flow rules**

```markdown
### Invite Flow
- **Existing Clerk user**: Direct add to org → `organization_users` (active) + `organization_user_roles` + invite record (accepted, audit trail) → HTTP 200
- **New user**: Create invite record (pending) + send Clerk invitation email → HTTP 201
- Expired invites cannot be accepted. Only pending invites can be canceled.
```

- [ ] **Step 3: Clarify HTTP method conventions**

Update the existing HTTP Methods section to emphasize:
```markdown
- PATCH: partial update, **all state transitions** (deactivate, reactivate, cancel, approve)
```

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add lifecycle rules, invite flow, and HTTP method clarifications to CLAUDE.md"
```

---

## Task 18: Run Full Build and Test Suite

- [ ] **Step 1: Full build**

```bash
dotnet build Propentro.slnx
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Run all tests**

```bash
dotnet test Propentro.slnx
```
Note: Requires Docker Desktop for integration tests. If Docker is unavailable, run unit tests only.

- [ ] **Step 3: Fix any failures and commit**

If there are test failures caused by the changes (e.g., tests that reference `IsSystemAdmin`, `Cancelled`, old route paths, or `HasActiveMembers`), fix them and commit:
```bash
git commit -m "fix(tests): update tests for production hardening changes"
```

---

## Verified Existing Behavior (No Changes Needed)

The following items from the spec were verified as already correct:
- **Clerk sync failure handling (spec 3.6)**: Existing handlers (e.g., `DeactivateMemberHandler` lines 68-73) already roll back DB changes when Clerk fails, returning `502 ExternalService`. No changes needed.
- **Permissions endpoint unbounded list (spec 3.3 item 27c)**: Permissions are system-defined and finite (~20 items). Not paginated per spec decision.
- **Existing invite flow audit trail (spec 3.5)**: `InviteToCurrentOrgHandler` already creates an `Accepted` invite record for existing users. Correct behavior.
