# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## System Context

This repo is part of the **Propentro** multi-repo ecosystem. See `../CLAUDE.md` for system-wide conventions, data flow, and cross-repo navigation.

- **Database schema:** `../propentro-db/releases/` — source of truth for table structure, column names (snake_case), CHECK constraints, and public ID prefixes
- **Frontend consumer:** `../propentro-app/src/app/api/` — Next.js proxy routes that forward requests to this API; endpoint changes here require matching proxy route updates
- **Frontend types:** `../propentro-app/src/features/` — TypeScript types that must match the Contracts DTOs from this API

## Build & Test Commands

```bash
# Build
dotnet build Propentro.slnx

# Run tests (requires Docker Desktop for integration tests)
dotnet test Propentro.slnx

# Run a single test class
dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~ClassName"

# Run a single test method
dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~ClassName.MethodName"

# CI-equivalent build
dotnet restore Propentro.slnx && dotnet build Propentro.slnx --configuration Release --no-restore && dotnet test Propentro.slnx --configuration Release --no-restore --no-build
```

## Build System

- **`TreatWarningsAsErrors` is enabled globally** — every warning is a build error. Fix all warnings before committing.
- `AnalysisLevel = latest-recommended` — aggressive CA rules. Suppress new rules in `.editorconfig` when needed.
- Centralized package management via `Directory.Packages.props` — add version there, reference without version in `.csproj`.
- Solution file is `Propentro.slnx` (not `.sln`).
- If you see nested `bin/Debug/net10.0/bin/Debug` paths, run `dotnet clean` and delete `bin/obj` directories.

## Architecture

**Layered structure** (`src/`):
- `Propentro.Api` — ASP.NET Core endpoints, middleware, Program.cs
- `Propentro.Application` — MediatR handlers (CQRS), FluentValidation validators, pipeline behaviors
- `Propentro.Infrastructure` — EF Core, Dapper queries, external services (Clerk auth, Resend email, S3 storage, Redis cache)
- `Propentro.Domain` — Entities, aggregate roots, domain events, value objects
- `Propentro.Contracts` — DTOs/response records shared between layers

**CQRS pattern**: Commands (`ICommand`/`ICommand<T>`) mutate state via EF Core. Queries (`IQuery<T>`/`ICachedQuery<T>`) read via Dapper. Never use EF Core for reads or Dapper for writes.

**MediatR pipeline order**: LoggingBehavior → QueryCachingBehavior (short-circuits on hit) → ValidationBehavior → UnitOfWorkBehavior (commands only).

**Result type**: Handlers return `Result<T>`. Use `Match()` for pattern matching. Implicit conversions from `T` and `Error`.

**Domain events & outbox**: Entities call `RaiseDomainEvent()` → events collected in `SaveChangesAsync()` → persisted to `outbox_messages` table → processed by Quartz job with exponential backoff retry. Handlers implement `IOutboxEventHandler`.

**Multi-tenancy**: Organization-scoped via `IUserContext.CurrentOrganizationId`. Global query filters on EF Core handle scoping and soft deletes automatically.

**Auth**: Clerk JWT → `UserContextMiddleware` resolves external IDs to internal DB IDs → `ActiveUserAuthorizationHandler` + `PermissionAuthorizationHandler`.

## Key Conventions

- **Dapper targets**: Use non-positional `sealed record` with `init` properties. Never use positional records as Dapper targets (constructor matching breaks with nullable params). Response types in `Contracts/` double as Dapper targets.
- **Private fields**: `_camelCase` prefix (enforced by `.editorconfig`).
- **Namespaces**: File-scoped.
- **Endpoints**: Grouped in `Api/Endpoints/`, composed via `MapApiEndpoints()`.
- **GlobalUsings.cs**: After adding a global using, grep for and remove duplicate explicit `using` statements across the project (prevents `IDE0005` errors).
- **Entity patterns**: `AggregateRoot` for aggregates, `OrganizationEntity` for tenant-scoped, `Entity` for base. Soft delete via `IsActive`. Audit fields via `IAuditableEntity`. Public IDs via `PublicIdGenerator` with entity-specific prefix.
- **Public ID naming in DTOs/Commands**: Never expose the term "PublicId" in Contracts, Commands, or API responses. The external world sees only `Id`, `UserId`, `RoleId`, `InviteId`, etc. The fact that these are public IDs is an internal implementation detail. Handlers resolve these IDs internally via `entity.PublicId == request.Id`. Internal logs may use `PublicId` in structured property names (e.g., `{UserPublicId}`) since they are not client-facing.

### Entity Lifecycle Rules
- **Organization**: Deactivate (`is_active = false`) or CASCADE hard delete (super admin only)
- **User**: Deactivate (`is_active = false`) or hard delete with SET NULL FKs (GDPR, super admin only)
- **Membership**: Deactivate (`status = 'deactivated'`, temporary) or hard delete (`organization_users` row deleted, permanent)
- Org deactivate → all members deactivated. Org reactivate → org active, members stay deactivated.
- Self-deactivation prohibited (`Member.CannotDeactivateSelf`)
- Hard deletes require `X-Confirm-Delete` header matching the resource public ID

### Invite Flow
- **Existing Clerk user**: Direct add to org → `organization_users` (active) + `organization_user_roles` + invite record (accepted, audit trail) → HTTP 200
- **New user**: Create invite record (pending) + send Clerk invitation email → HTTP 201
- Expired invites cannot be accepted. Only pending invites can be canceled.

## Testing

- **Frameworks**: xUnit, FluentAssertions, NSubstitute
- **Integration tests**: Testcontainers with `PostgreSqlBuilder("postgres:17-alpine")`. Requires Docker Desktop running. Uses `IClassFixture` — tests share one server; avoid destructive DB operations.
- **Test naming**: Underscores allowed (CA1707 suppressed in `/tests/.editorconfig`).
- Integration tests use `InternalsVisibleTo` — `Program` is `public partial class` in Api.csproj.

## Error Handling

### Two Error Paths
1. **Result path** — handler returns `Result<T>` with `Error` → `ToHttpResult()` maps to ProblemDetails
2. **Exception path** — `GlobalExceptionHandler` catches unhandled exceptions → ProblemDetails

Both paths produce consistent ProblemDetails with `type` URI, `code`, `timestamp`, and optional `details`.

### GlobalExceptionHandler Mapping
`ValidationException` → 400, `DbUpdateConcurrencyException` → 409, `OperationCanceledException` → 499, unhandled → 500.

### Error Type → HTTP Status
| `ErrorType` | HTTP Status | Use for |
|---|---|---|
| `Validation` | 400 | Input validation failures |
| `Unauthorized` | 401 | Not authenticated |
| `Forbidden` | 403 | Not authorized |
| `NotFound` | 404 | Resource not found |
| `Conflict` | 409 | State-guard failures (`AlreadyInactive`, `AlreadyActive`, `NotCancellable`, `SlugTaken`) |
| `ExternalService` | 502 | Third-party API failures (Clerk, email, storage) |

### Error Code Format
- Dot-notation PascalCase: `Entity.ErrorName` (e.g., `Organization.NotFound`, `Invite.AlreadyPending`)
- Exception handler codes: `Validation.Failed`, `Concurrency.Conflict`, `Request.Canceled`, `Internal.ServerError`
- Never use SCREAMING_SNAKE_CASE codes

### Error Classes
- Define errors as `public static readonly Error` fields in dedicated error classes
- Shared/cross-feature errors go in `Application/Common/Errors/` (e.g., `OrganizationErrors`, `ConfirmationErrors`)
- Feature-specific errors go next to their handlers (e.g., `MemberErrors`, `InviteErrors`, `AdminUserErrors`)
- Never construct errors inline in handlers — always reference an error class field
- Field name must match the error code suffix (e.g., `AlreadyPending` for code `Invite.AlreadyPending`)
- If the same error code appears in multiple classes, consolidate into `Common/Errors/`

### External Service Errors (Info Leak Prevention)
- Never expose third-party service names (Clerk, Resend, etc.) in error codes or messages sent to clients
- Use generic codes: `ExternalService.NotConfigured`, `ExternalService.Unavailable`, `ExternalService.SyncFailed`
- Use generic messages: `"An external service is temporarily unavailable. Please try again later."`
- Raw error bodies and `ex.Message` belong in server logs (Serilog), never in client responses
- Application-layer errors referencing sync failures use `Entity.SyncFailed` (e.g., `Organization.SyncFailed`, `Member.SyncFailed`)
- Never return raw infrastructure `Result` to the client — wrap in a domain-specific error
