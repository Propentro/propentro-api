# Error Handling Consistency Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make all API error responses consistent ProblemDetails with no info leaks, using dot-notation PascalCase codes everywhere.

**Architecture:** Two error shapes — validation (400 with field-level `errors` dict) and business/system (4xx/5xx with `code` + `detail`). Both include `type` URI, `code`, `timestamp`. No third-party service names exposed to clients. All state-guard errors use `Conflict` (409).

**Tech Stack:** ASP.NET Core, MediatR, FluentValidation, ProblemDetails (RFC 9457)

---

### Task 1: Unify `ResultExtensions.ToHttpResult()` — add `type` URI

**Files:**
- Modify: `src/Propentro.Api/Extensions/ResultExtensions.cs:31-65`

Currently `ResultExtensions.ToHttpResult()` produces ProblemDetails without a `type` URI, while `GlobalExceptionHandler` includes one. Both paths must match.

- [ ] **Step 1: Add `type` URI to `ToHttpResult`**

Add a `GetTypeUri` method and pass it to `Results.Problem()`. Also remove `details` extension when null (avoid sending `"details": null`).

```csharp
public static IResult ToHttpResult(this Error error)
{
    var statusCode = error.Type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.ExternalService => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status400BadRequest
    };

    var extensions = new Dictionary<string, object?>
    {
        ["code"] = error.Code,
        ["timestamp"] = DateTimeOffset.UtcNow
    };

    if (error.Details is not null)
        extensions["details"] = error.Details;

    return Results.Problem(
        type: GetTypeUri(error.Type),
        statusCode: statusCode,
        title: GetTitle(error.Type),
        detail: error.Message,
        extensions: extensions);
}

private static string GetTypeUri(ErrorType type) => type switch
{
    ErrorType.Failure => "https://propentro.com/errors/failure",
    ErrorType.NotFound => "https://propentro.com/errors/not-found",
    ErrorType.Unauthorized => "https://propentro.com/errors/unauthorized",
    ErrorType.Forbidden => "https://propentro.com/errors/forbidden",
    ErrorType.Validation => "https://propentro.com/errors/validation",
    ErrorType.Conflict => "https://propentro.com/errors/conflict",
    ErrorType.ExternalService => "https://propentro.com/errors/external-service",
    _ => "https://propentro.com/errors/error"
};
```

- [ ] **Step 2: Build and verify no warnings**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Api/Extensions/ResultExtensions.cs
git commit -m "feat: add type URI and clean up null details in ResultExtensions"
```

---

### Task 2: Unify `GlobalExceptionHandler` codes to dot-notation

**Files:**
- Modify: `src/Propentro.Api/Middleware/GlobalExceptionHandler.cs:53,71,88,107`

Change SCREAMING_SNAKE_CASE codes to dot-notation PascalCase to match the Result path.

- [ ] **Step 1: Update exception handler codes**

| Old | New |
|---|---|
| `VALIDATION_FAILED` | `Validation.Failed` |
| `CONCURRENCY_CONFLICT` | `Concurrency.Conflict` |
| `REQUEST_CANCELED` | `Request.Canceled` |
| `INTERNAL_ERROR` | `Internal.ServerError` |

Change only the `["code"]` values on lines 53, 71, 88, 107.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Api/Middleware/GlobalExceptionHandler.cs
git commit -m "refactor: unify exception handler codes to dot-notation PascalCase"
```

---

### Task 3: Add `ConfirmationErrors` class and fix inline `Results.Problem()` calls

**Files:**
- Create: `src/Propentro.Application/Common/Errors/ConfirmationErrors.cs`
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminUserEndpoints.cs:85-88`
- Modify: `src/Propentro.Api/Endpoints/Admin/AdminOrganizationEndpoints.cs:115-118`

The inline `Results.Problem()` calls lack `code` and `timestamp`. Move these to an error class and use `ToHttpResult()`.

- [ ] **Step 1: Create `ConfirmationErrors.cs`**

```csharp
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Errors;

public static class ConfirmationErrors
{
    public static readonly Error DeleteNotConfirmed = Error.Validation(
        "Confirmation.DeleteNotConfirmed",
        "X-Confirm-Delete header must match the resource ID.");
}
```

- [ ] **Step 2: Update `AdminUserEndpoints.cs`**

Replace lines 85-88:
```csharp
// Before:
return Results.Problem(
    statusCode: 400,
    title: "Confirmation Required",
    detail: "X-Confirm-Delete header must match the user ID.");

// After:
return ConfirmationErrors.DeleteNotConfirmed.ToHttpResult();
```

Add the necessary usings at the top of the file.

- [ ] **Step 3: Update `AdminOrganizationEndpoints.cs`**

Replace lines 115-118:
```csharp
// Before:
return Results.Problem(
    statusCode: 400,
    title: "Confirmation Required",
    detail: "X-Confirm-Delete header must match the organization ID.");

// After:
return ConfirmationErrors.DeleteNotConfirmed.ToHttpResult();
```

Add the necessary usings at the top of the file.

- [ ] **Step 4: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Propentro.Application/Common/Errors/ConfirmationErrors.cs \
       src/Propentro.Api/Endpoints/Admin/AdminUserEndpoints.cs \
       src/Propentro.Api/Endpoints/Admin/AdminOrganizationEndpoints.cs
git commit -m "refactor: replace inline Results.Problem with ConfirmationErrors"
```

---

### Task 4: Remove info leaks from `ClerkOrganizationService`

**Files:**
- Modify: `src/Propentro.Infrastructure/Authentication/ClerkOrganizationService.cs`

Two problems: (1) error codes expose "Clerk" service name, (2) `ErrorType.Failure` → 400 instead of `ErrorType.ExternalService` → 502. Also, error messages include raw Clerk API response bodies — info leak.

- [ ] **Step 1: Add private static error fields to reduce duplication**

Add these at the top of the class to avoid repeating the same error construction in every method:

```csharp
private static readonly Error NotConfiguredError = Error.ExternalService(
    "ExternalService.NotConfigured",
    "An external service is not properly configured. Please contact support.");

private static readonly Error UnavailableError = Error.ExternalService(
    "ExternalService.Unavailable",
    "An external service is temporarily unavailable. Please try again later.");

private static readonly Error SyncFailedError = Error.ExternalService(
    "ExternalService.SyncFailed",
    "An external service is temporarily unavailable. Please try again later.");
```

- [ ] **Step 2: Update all error codes and types across ALL methods**

Every method in this file must be updated. The mapping:

| Old Code | Old Type | New Field |
|---|---|---|
| `Clerk.NotConfigured` (8 occurrences in all methods including `RevokeInvitationAsync`) | `Failure` | `NotConfiguredError` |
| `Clerk.UpdateFailed` | `Failure` | `SyncFailedError` |
| `Clerk.Unavailable` (7 occurrences in all catch blocks including `RevokeInvitationAsync`) | `Failure` | `UnavailableError` |
| `Clerk.InvitationFailed` | `Failure` | `SyncFailedError` |
| `Clerk.AddMemberFailed` | `Failure` | `SyncFailedError` |
| `Clerk.RemoveMemberFailed` | `Failure` | `SyncFailedError` |
| `Clerk.CreateOrgFailed` | `Failure` | `SyncFailedError` |
| `Clerk.DeleteOrgFailed` | `Failure` | `SyncFailedError` |
| `Clerk.DeleteUserFailed` | `Failure` | `SyncFailedError` |
| `Member.AlreadyInvited` | `Conflict` | `Invite.AlreadyPending` (align with InviteErrors code) |

**Important:** `RevokeInvitationAsync` (line 258) also has `Clerk.NotConfigured` — don't miss it. Although it currently returns `Result.Success()` on most failures (best-effort), the `NotConfigured` check at the top still returns a failure result.

Replace raw `body` and `ex.Message` in error messages with the private static fields. The raw body is already being logged via `Log.Warning` — that's where the detail belongs, not in the client response.

Example for each error:
```csharp
// Before:
return Result.Failure(new Error("Clerk.UpdateFailed", body, ErrorType.Failure));

// After:
return Result.Failure(Error.ExternalService(
    "ExternalService.SyncFailed",
    "An external service is temporarily unavailable. Please try again later."));
```

For `Clerk.NotConfigured`:
```csharp
// Before:
return Result.Failure(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

// After:
return Result.Failure(Error.ExternalService(
    "ExternalService.NotConfigured",
    "An external service is not properly configured. Please contact support."));
```

For `Clerk.Unavailable` (HttpRequestException catches):
```csharp
// Before:
return Result.Failure(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));

// After:
return Result.Failure(Error.ExternalService(
    "ExternalService.Unavailable",
    "An external service is temporarily unavailable. Please try again later."));
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Infrastructure/Authentication/ClerkOrganizationService.cs
git commit -m "fix: remove Clerk info leaks, use ExternalService error type for all third-party failures"
```

---

### Task 5: Remove "Clerk" from Application-layer error codes and messages

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Settings/OrganizationSettingsErrors.cs:12-14`
- Modify: `src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs:13-14`
- Modify: `src/Propentro.Application/Features/Admin/Users/AdminUserErrors.cs:22-23`
- Modify: `src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs:19-20`

- [ ] **Step 1: Update error codes and messages**

| File | Old Code | New Code | New Message |
|---|---|---|---|
| `OrganizationSettingsErrors.cs` | `Organization.ClerkSyncFailed` | `Organization.SyncFailed` | `"Failed to sync organization settings. Please try again."` |
| `AdminOrganizationErrors.cs` | `Organization.ClerkSyncFailed` | `Organization.SyncFailed` | `"Failed to sync organization. Please try again."` |
| `AdminUserErrors.cs` | `AdminUser.ClerkSyncFailed` | `AdminUser.SyncFailed` | `"Failed to sync user. Please try again."` |
| `InviteErrors.cs` | `Invite.ClerkSyncFailed` | `Invite.SyncFailed` | `"Failed to process invite. Please try again."` |

- [ ] **Step 2: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Settings/OrganizationSettingsErrors.cs \
       src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs \
       src/Propentro.Application/Features/Admin/Users/AdminUserErrors.cs \
       src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs
git commit -m "refactor: remove Clerk references from application-layer error codes"
```

---

### Task 6: Fix inline `Organization.ClerkNotLinked` errors

**Files:**
- Create: `src/Propentro.Application/Common/Errors/OrganizationErrors.cs`
- Modify: `src/Propentro.Application/Features/Admin/Invites/Commands/InviteUser/InviteUserHandler.cs:92,143`
- Modify: `src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgHandler.cs:26`

The inline `Organization.ClerkNotLinked` errors (a) expose "Clerk", (b) use `ErrorType.Validation` (400) for what's an infrastructure issue, (c) are duplicated 3 times.

- [ ] **Step 1: Create shared `OrganizationErrors.cs`**

```csharp
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Errors;

public static class OrganizationErrors
{
    public static readonly Error NotLinkedToAuthProvider = Error.ExternalService(
        "Organization.NotLinkedToAuthProvider",
        "Organization is not linked to the authentication provider. Please contact support.");
}
```

- [ ] **Step 2: Replace inline errors in `InviteUserHandler.cs`**

Replace both occurrences (lines 92 and 143):
```csharp
// Before:
return Error.Validation("Organization.ClerkNotLinked", "Organization is not linked to Clerk. Cannot send invite.");

// After:
return OrganizationErrors.NotLinkedToAuthProvider;
```

Add using: `using Propentro.Application.Common.Errors;`

- [ ] **Step 3: Replace inline error in `InviteToCurrentOrgHandler.cs`**

Replace line 26:
```csharp
// Before:
return Error.Validation("Organization.ClerkNotLinked", "Organization is not linked to Clerk. Cannot send invite.");

// After:
return OrganizationErrors.NotLinkedToAuthProvider;
```

Add using: `using Propentro.Application.Common.Errors;`

- [ ] **Step 4: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Propentro.Application/Common/Errors/OrganizationErrors.cs \
       src/Propentro.Application/Features/Admin/Invites/Commands/InviteUser/InviteUserHandler.cs \
       src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgHandler.cs
git commit -m "refactor: centralize Organization.NotLinkedToAuthProvider error, remove Clerk reference"
```

---

### Task 7: Fix inline duplicate errors in query handlers

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Members/Queries/GetMember/GetMemberHandler.cs:37`
- Modify: `src/Propentro.Application/Features/Organizations/Roles/Queries/GetRole/GetRoleHandler.cs:39`
- Modify: `src/Propentro.Application/Features/Admin/Me/Queries/GetAdminMe/GetAdminMeHandler.cs:37`

These handlers construct errors inline that duplicate existing error class definitions.

- [ ] **Step 1: Replace inline errors with error class references**

`GetMemberHandler.cs` line 37:
```csharp
// Before:
return Error.NotFound("Member.NotFound", "Member not found.");

// After:
return MemberErrors.NotFound;
```

`GetRoleHandler.cs` line 39:
```csharp
// Before:
return Error.NotFound("Role.NotFound", "Role not found.");

// After:
return RoleErrors.NotFound;
```

`GetAdminMeHandler.cs` line 37 — this one uses `Admin.NotFound` which has no error class. Add to `MeErrors.cs` using the `Me.` prefix to match the class convention:
```csharp
// Add to MeErrors.cs:
public static readonly Error AdminNotFound = new(
    "Me.AdminNotFound",
    "Admin profile not found.",
    ErrorType.NotFound);
```

Then in `GetAdminMeHandler.cs`:
```csharp
// Before:
return Error.NotFound("Admin.NotFound", "Admin profile not found.");

// After:
return MeErrors.AdminNotFound;
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Members/Queries/GetMember/GetMemberHandler.cs \
       src/Propentro.Application/Features/Organizations/Roles/Queries/GetRole/GetRoleHandler.cs \
       src/Propentro.Application/Features/Admin/Me/Queries/GetAdminMe/GetAdminMeHandler.cs \
       src/Propentro.Application/Features/Me/MeErrors.cs
git commit -m "refactor: replace inline error construction with error class references"
```

---

### Task 8: Fix state-guard error type inconsistencies

**Files:**
- Modify: `src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs:16-17`
- Modify: `src/Propentro.Application/Features/Admin/Users/AdminUserErrors.cs:10-11,13-14`
- Modify: `src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs:25-26`

State-guard errors ("already inactive", "already active", "not cancellable because not pending") should all use `Conflict` (409), not `Validation` (400).

- [ ] **Step 1: Fix `AdminOrganizationErrors.AlreadyInactive`**

```csharp
// Before:
public static readonly Error AlreadyInactive = Error.Validation(
    "Organization.AlreadyInactive", "Organization is already inactive.");

// After:
public static readonly Error AlreadyInactive = Error.Conflict(
    "Organization.AlreadyInactive", "Organization is already inactive.");
```

- [ ] **Step 2: Fix `AdminUserErrors.AlreadyInactive` and `AlreadyActive`**

```csharp
// Before:
public static readonly Error AlreadyInactive = Error.Validation(
    "AdminUser.AlreadyInactive", "User is already inactive.");
public static readonly Error AlreadyActive = Error.Validation(
    "AdminUser.AlreadyActive", "User is already active.");

// After:
public static readonly Error AlreadyInactive = Error.Conflict(
    "AdminUser.AlreadyInactive", "User is already inactive.");
public static readonly Error AlreadyActive = Error.Conflict(
    "AdminUser.AlreadyActive", "User is already active.");
```

- [ ] **Step 3: Fix `InviteErrors.NotCancellable`**

```csharp
// Before:
public static readonly Error NotCancellable = Error.Validation(
    "Invite.NotCancellable", "Only pending invites can be canceled.");

// After:
public static readonly Error NotCancellable = Error.Conflict(
    "Invite.NotCancellable", "Only pending invites can be canceled.");
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs \
       src/Propentro.Application/Features/Admin/Users/AdminUserErrors.cs \
       src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs
git commit -m "fix: use Conflict (409) for all state-guard errors consistently"
```

---

### Task 9: Fix `DeactivateMemberHandler` raw Clerk result leak

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs:72-76`

This handler returns the raw `clerkResult` which exposes infrastructure error codes and messages.

- [ ] **Step 1: Add `SyncFailed` error to `MemberErrors.cs`**

```csharp
// Add to MemberErrors.cs:
public static readonly Error SyncFailed = Error.ExternalService(
    "Member.SyncFailed",
    "Failed to sync member changes. Please try again.");
```

- [ ] **Step 2: Wrap the raw result in `DeactivateMemberHandler.cs`**

```csharp
// Before (lines 72-76):
var clerkResult = await clerkService.RemoveMemberAsync(org.ExternalId, user.ExternalId, cancellationToken);
if (clerkResult.IsFailure)
{
    Log.Warning("Failed to remove user {UserId} from Clerk org — aborting deactivation", user.PublicId);
    return clerkResult;
}

// After:
var clerkResult = await clerkService.RemoveMemberAsync(org.ExternalId, user.ExternalId, cancellationToken);
if (clerkResult.IsFailure)
{
    Log.Warning("Failed to remove user {UserId} from Clerk org — aborting deactivation", user.PublicId);
    return MemberErrors.SyncFailed;
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Members/MemberErrors.cs \
       src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs
git commit -m "fix: wrap raw Clerk result in MemberErrors.SyncFailed to prevent info leak"
```

---

### Task 10: Fix `InviteErrors.AlreadyInvited` / `Invite.AlreadyPending` mismatch

**Files:**
- Modify: `src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs:16-17`

Field name is `AlreadyInvited` but error code says `Invite.AlreadyPending`. Align them.

- [ ] **Step 1: Rename field to match code**

```csharp
// Before:
public static readonly Error AlreadyInvited = Error.Conflict(
    "Invite.AlreadyPending", "A pending invite already exists for this email and organization.");

// After:
public static readonly Error AlreadyPending = Error.Conflict(
    "Invite.AlreadyPending", "A pending invite already exists for this email and organization.");
```

- [ ] **Step 2: Find and update all references to `InviteErrors.AlreadyInvited`**

Run: `grep -r "InviteErrors.AlreadyInvited" src/` and update each reference to `InviteErrors.AlreadyPending`.

- [ ] **Step 3: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 4: Commit**

Stage the specific files found in Step 2 plus the InviteErrors.cs file:
```bash
git add src/Propentro.Application/Features/Admin/Invites/InviteErrors.cs
# Also add any handler files that referenced InviteErrors.AlreadyInvited
git commit -m "refactor: rename InviteErrors.AlreadyInvited to AlreadyPending to match error code"
```

---

### Task 11: Resolve duplicate `Organization.NotFound` and `Organization.ClerkSyncFailed` across error classes

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Settings/OrganizationSettingsErrors.cs`
- Modify: `src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs`
- Modify: `src/Propentro.Application/Common/Errors/OrganizationErrors.cs` (created in Task 6)

`Organization.NotFound` is defined in both `OrganizationSettingsErrors` and `AdminOrganizationErrors`. After Task 5, `Organization.SyncFailed` will also be in both. Consolidate into the shared `OrganizationErrors` class.

- [ ] **Step 1: Move shared errors to `OrganizationErrors.cs`**

Add to the `OrganizationErrors.cs` created in Task 6:

```csharp
public static readonly Error NotFound = Error.NotFound(
    "Organization.NotFound", "Organization not found.");

public static readonly Error SyncFailed = Error.ExternalService(
    "Organization.SyncFailed", "Failed to sync organization. Please try again.");
```

- [ ] **Step 2: Update `OrganizationSettingsErrors.cs`**

Remove `NotFound` and `ClerkSyncFailed` (now `SyncFailed`). Reference `OrganizationErrors` instead.

If `OrganizationSettingsErrors` has no remaining errors, delete the file entirely. Update all references in handlers to use `OrganizationErrors.NotFound` and `OrganizationErrors.SyncFailed`.

- [ ] **Step 3: Update `AdminOrganizationErrors.cs`**

Remove `NotFound` and `ClerkSyncFailed`/`SyncFailed`. Keep `SlugTaken`, `AlreadyInactive`, `AlreadyActive`. Update handler references to use `OrganizationErrors.NotFound` and `OrganizationErrors.SyncFailed`.

- [ ] **Step 4: Find and update all handler references**

Search for `OrganizationSettingsErrors.NotFound`, `OrganizationSettingsErrors.ClerkSyncFailed` (or `SyncFailed`), `AdminOrganizationErrors.NotFound`, `AdminOrganizationErrors.ClerkSyncFailed` (or `SyncFailed`) and replace with `OrganizationErrors.*`.

- [ ] **Step 5: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 6: Commit**

Stage all modified files explicitly:
```bash
git add src/Propentro.Application/Common/Errors/OrganizationErrors.cs \
       src/Propentro.Application/Features/Organizations/Settings/OrganizationSettingsErrors.cs \
       src/Propentro.Application/Features/Admin/Organizations/AdminOrganizationErrors.cs
# Also add any handler files updated in Step 4
git commit -m "refactor: consolidate duplicate Organization errors into shared OrganizationErrors class"
```

> **Note:** This task depends on Task 5 (ClerkSyncFailed → SyncFailed rename) and Task 6 (OrganizationErrors.cs creation). Do not reorder.

---

### Task 12: Update `Error.Unexpected` to use dot-notation

**Files:**
- Modify: `src/Propentro.Application/Common/Models/Error.cs:19`

- [ ] **Step 1: Change `UNEXPECTED_ERROR` to `Internal.UnexpectedError`**

```csharp
// Before:
public static Error Unexpected(string message) =>
    new("UNEXPECTED_ERROR", message);

// After:
public static Error Unexpected(string message) =>
    new("Internal.UnexpectedError", message);
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Application/Common/Models/Error.cs
git commit -m "refactor: change Error.Unexpected code to dot-notation"
```

---

### Task 13: Run full test suite

- [ ] **Step 1: Run all tests**

Run: `dotnet test Propentro.slnx`
Expected: All tests pass.

- [ ] **Step 2: Fix any failures**

If tests reference old error codes or messages, update them to match.

- [ ] **Step 3: Final commit if tests needed updating**

Stage only the modified test files explicitly:
```bash
git add tests/
git commit -m "test: update tests for new error codes and types"
```
