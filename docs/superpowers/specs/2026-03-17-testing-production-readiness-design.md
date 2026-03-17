# Testing & Production Readiness — Design Spec

**Date:** 2026-03-17
**Scope:** Test infrastructure, structural improvements, CI enforcement
**Status:** Reviewed

## Context

The Propentro API has a solid architecture (CQRS, domain events, outbox, rate limiting, health checks, auth) but zero tests written. Three test projects exist with csproj files but no test classes. For an AI-assisted development workflow (Claude Code sessions), tests are the primary safety net between sessions — a session can break code that a previous session wrote, and only automated tests catch this.

## Phase 1: Testing & Production Readiness (Now)

### 1. Integration Tests

**Project:** `tests/Propentro.Api.Tests/`
**Purpose:** Test full HTTP pipeline with real database. Highest ROI — catches Dapper query bugs, middleware issues, auth flow problems, serialization errors.

#### Test Infrastructure

```
tests/Propentro.Api.Tests/
  Shared/
    PropentroApiFactory.cs      -- WebApplicationFactory + Testcontainers PostgreSQL
    TestConstants.cs            -- Known seed data IDs, reusable values
    DatabaseSeeder.cs           -- Consistent seed data per test class
  Features/
    Organizations/
      Settings/
      Members/
      Roles/
      Invites/
    Admin/
      Organizations/
      Users/
      Members/
      Invites/
      Me/
    Webhooks/
    Me/
```

#### PropentroApiFactory Design

- Extends `WebApplicationFactory<Program>`
- Replaces real PostgreSQL connection with Testcontainers instance
- Bypasses Clerk JWT auth — injects fake claims (configurable user ID, org ID, permissions)
- Replaces external services with fakes/stubs (Clerk API, Resend email, S3 storage)
- Implements `IClassFixture<PropentroApiFactory>` — one DB per test class, not per test
- Runs EF Core migrations on startup to match real schema
- `DatabaseSeeder` populates known state: test organization, users, roles, permissions, memberships

#### Auth Bypass Strategy

Tests need to simulate different auth states:
- Authenticated user with specific org membership and permissions
- System administrator
- Unauthenticated request
- User in different organization (multi-tenancy isolation test)

**Approach — two layers bypassed:**

1. **JWT Authentication:** Custom `AuthenticationHandler<AuthenticationSchemeOptions>` registered in test factory. Reads test identity from custom headers (e.g., `X-Test-User-Id`, `X-Test-Org-Id`). Always succeeds with a `ClaimsPrincipal` containing the external user/org claims that the real Clerk JWT would carry. No real JWT validation.

2. **UserContextMiddleware:** Runs normally in tests — it calls `IExternalIdResolver` to map external IDs → internal DB GUIDs. Since the test DB is seeded with known users/orgs whose external IDs match the test claims, the middleware resolves correctly and populates `IUserContext` with real internal GUIDs.

**Per-test identity switching:** Factory exposes a helper method:
```csharp
public HttpClient CreateClientAs(string externalUserId, string externalOrgId)
```
Returns an `HttpClient` with the test auth headers pre-set. Tests call this to simulate different users/orgs. For unauthenticated requests, use `CreateClient()` without headers — the custom auth handler returns `AuthenticateResult.NoResult()` when headers are missing.

**Seed data contract:** `DatabaseSeeder` creates users with known external IDs (e.g., `"clerk_test_user_1"`, `"clerk_test_org_1"`) that match the values used by `CreateClientAs`. `TestConstants` holds these as constants.

#### Test Coverage Per Endpoint

Each endpoint gets tests for:
- **Happy path** — valid request, expected response body and status
- **Validation errors** — missing/invalid fields → 400
- **Not found** — nonexistent resource → 404
- **Forbidden** — missing permission → 403
- **Business rule violation** — state guards (e.g., deactivate already-inactive member → 409)
- **Multi-tenancy isolation** — org A cannot see org B's data

#### Seed Data Strategy

- `DatabaseSeeder` creates a known state before tests run
- Two organizations (test isolation), multiple users, roles with specific permissions, invites in various states
- Tests read from known IDs (defined in `TestConstants`)
- Tests that mutate data must not break other tests in the same class — use unique records or restore state

### 2. Unit Tests

**Project:** `tests/Propentro.Domain.Tests/`
**Purpose:** Test domain entity logic — state transitions, validation, domain event raising.

```
tests/Propentro.Domain.Tests/
  Entities/
    OrganizationTests.cs       -- Deactivate, Reactivate, domain events
    InviteTests.cs             -- Accept, Cancel, expiry checks
    OrganizationUserTests.cs   -- Status transitions
    RoleTests.cs               -- Permission management
    PropertyTests.cs           -- Creation, update events
    ContactTests.cs            -- Archive, delete events
  ValueObjects/
    MoneyTests.cs
    DateRangeTests.cs
  PublicIdGeneratorTests.cs
```

**Rule:** Only test classes with real logic. No tests for DTOs, enums, or pass-through code.

**Project:** `tests/Propentro.Application.Tests/`
**Purpose:** Test validators and shared services in isolation.

```
tests/Propentro.Application.Tests/
  Validators/
    CreateOrganizationValidatorTests.cs
    InviteUserValidatorTests.cs
    UpdateOrganizationSettingsValidatorTests.cs
    CreateRoleValidatorTests.cs
    ...
  Services/
    InviteServiceTests.cs
```

**Approach:** Validators tested with valid/invalid inputs. Services tested with NSubstitute mocks for dependencies.

### 3. Architecture Tests

**Project:** `tests/Propentro.Architecture.Tests/` (new project)
**Purpose:** Enforce layer dependency rules and CQRS conventions automatically. Prevents AI sessions from accidentally breaking architecture.
**Library:** NetArchTest.Rules

#### Layer Dependency Rules

| Layer | Can Reference | Cannot Reference |
|---|---|---|
| Domain | Nothing | Application, Infrastructure, Api, Contracts |
| Contracts | Nothing | Domain, Application, Infrastructure, Api |
| Application | Domain, Contracts | Infrastructure, Api |
| Infrastructure | Domain, Application | Api, Contracts (no direct reference) |
| Api | All | — |

#### CQRS Convention Rules

- Classes implementing `ICommand` / `ICommand<T>` handlers must not reference `IDbConnectionFactory` (no Dapper in commands)
- Classes implementing `IQuery<T>` handlers must not reference `IAppDbContext` (no EF Core in queries)
- All command/query records must be sealed
- All handler classes must be sealed and internal

### 4. Correlation ID

**Purpose:** Track a single request across all log entries. Debug production issues by filtering logs with one ID.

**Implementation:**
- Added to `UserContextMiddleware` (no separate middleware)
- Check incoming `X-Correlation-Id` header → use if present, generate UUID if not
- Push to Serilog `LogContext` as `CorrelationId` property
- Set `X-Correlation-Id` on response header
- Handlers access it via Serilog `LogContext` — no changes to `IUserContext` interface needed

### 5. Health Check Expansion

**Current:** PostgreSQL only at `/health`.

**Changes:**
- `/health` — lightweight liveness check (app is running)
- `/health/ready` — readiness check: PostgreSQL + Redis connectivity
- Redis health check added via `AspNetCore.HealthChecks.Redis` package
- Both endpoints excluded from auth (already the case for `/health`)
- Railway uses `/health/ready` for container health decisions

### 6. Structured Logging Improvements

**Current:** Serilog with console sink, enrichment, request logging.

**Changes:**
- JSON formatter for production (Railway parses structured JSON logs)
- Console (human-readable) formatter for development
- Switch based on environment: `ASPNETCORE_ENVIRONMENT`
- Sensitive data destructuring policy — mask connection strings, API keys, tokens if they appear in log properties
- No request/response body logging (security risk, not needed)

### 7. Automatic Cache Invalidation

**Purpose:** Commands that change data should automatically clear related cache entries without manual code in each handler.

**Design:**

New interface in `Application/Common/Interfaces/Caching/`:
```csharp
public interface ICacheInvalidating
{
    IReadOnlyList<string> CacheKeysToInvalidate { get; }
}
```

**Behavior:** `UnitOfWorkBehavior` gains two new dependencies: `ICacheService` and `IUserContext` (both already registered in DI). After successful `SaveChangesAsync`:
1. Check if command implements `ICacheInvalidating`
2. If yes, iterate `CacheKeysToInvalidate` — these are bare key names (e.g., `"organization-settings"`)
3. Behavior prefixes each key with `org:{IUserContext.OrganizationId}:` (same logic as `QueryCachingBehavior`)
4. Call `ICacheService.RemoveAsync` for each prefixed key

**Admin commands** (which operate cross-org with an explicit `organizationId` parameter): These must return fully-prefixed keys themselves, including the org ID. The interface adds a flag:
```csharp
public interface ICacheInvalidating
{
    IReadOnlyList<string> CacheKeysToInvalidate { get; }
    bool KeysAreFullyQualified => false; // default: behavior adds org prefix
}
```
When `KeysAreFullyQualified` is `true`, the behavior skips prefixing and uses keys as-is.

**Usage — org-scoped command** (bare keys, prefix added automatically):
```csharp
public sealed record UpdateOrganizationSettings(...) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate =>
        ["organization-settings"];
}
```

**Usage — admin command** (fully-qualified keys):
```csharp
public sealed record DeactivateOrganization(Guid OrganizationId) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate =>
        [$"org:{OrganizationId}:organization-settings"];
    public bool KeysAreFullyQualified => true;
}
```

Commands that don't implement `ICacheInvalidating` are unaffected — zero changes needed.

### 8. CI Test Enforcement

**Changes to existing GitHub Actions workflow:**
- Add `dotnet test Propentro.slnx --configuration Release --no-restore --no-build` step after build
- Tests must pass before PR can merge
- Architecture tests run in same pipeline (they're just another test project)

---

## Phase 2: Post-Launch (tracked in docs/TODO.md)

These items are not implemented now. They become valuable when real users generate traffic.

- **OpenTelemetry** — Distributed tracing and metrics export. Answers "which endpoint is slow and why?" with request-level traces through middleware → handler → DB. Connect to Grafana, Jaeger, or Datadog.
- **Response Compression** — gzip/brotli middleware. Reduces payload size for list endpoints. Two lines of code: `AddResponseCompression()` + `UseResponseCompression()`. Measurable impact only with real traffic patterns.

---

## Implementation Notes

### Test Project Dependencies

| Test Project | References | Packages to Add | Notes |
|---|---|---|---|
| `Propentro.Api.Tests` | Api (transitively includes all) | Testcontainers.PostgreSql, Microsoft.AspNetCore.Mvc.Testing | Already has xUnit, FluentAssertions, NSubstitute |
| `Propentro.Application.Tests` | Application | NSubstitute | Already has xUnit, FluentAssertions |
| `Propentro.Domain.Tests` | Domain | (none extra) | Already has xUnit, FluentAssertions |
| `Propentro.Architecture.Tests` (new) | All src projects | NetArchTest.Rules, xUnit, FluentAssertions | New csproj, must be added to `Propentro.slnx` |

### Ordering

1. Architecture tests first (fast, no infrastructure needed, protects everything after)
2. Test infrastructure (PropentroApiFactory, seeder, auth bypass)
3. Integration tests for existing endpoints
4. Unit tests for domain entities and validators
5. Structural improvements (correlation ID, health checks, logging, cache invalidation)
6. CI pipeline update
