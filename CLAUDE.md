# CLAUDE.md - Propentro API Guide

Propentro: .NET 10.0 property management API, Clean Architecture, multi-tenant, CQRS.

## Tech Stack

| Category | Technologies |
|----------|-------------|
| Framework | .NET 10.0, ASP.NET Core |
| Database | PostgreSQL, EF Core 10.0.2, Dapper |
| Patterns | CQRS, MediatR 14.0, Result Pattern, DDD, UnitOfWork |
| Validation | FluentValidation 12.1 |
| Auth | Clerk JWT (Bearer tokens) |
| Logging | Serilog (static `Log` class — NOT injected ILogger) |
| Testing | xUnit, FluentAssertions, NSubstitute, Testcontainers |
| Caching | Redis (default) / MemoryCache (fallback) via `ICacheService` |
| Email | Resend via HTTP + Polly resilience |
| Storage | S3-compatible (Cloudflare R2) via AWSSDK.S3 |
| Background Jobs | Quartz.NET 3.15 (outbox processing) |
| Resilience | Polly — custom `AddStandardResilience()` in `Infrastructure/Resilience/ResiliencePolicies.cs` |
| Observability | OpenTelemetry (OTLP, ASP.NET Core + HTTP + Runtime) |

## Architecture

**Layer dependencies:** Api → Application + Infrastructure + Contracts; Application → Domain + Contracts; Infrastructure → Domain + Application; Domain → none; Contracts → none.

## Project Structure

```
src/
├── Propentro.Api/
│   ├── Endpoints/          # Feature-based endpoint definitions
│   ├── Middleware/          # GlobalExceptionHandler, UserContextMiddleware
│   ├── Extensions/          # ResultExtensions (IResult conversion)
│   └── Program.cs
├── Propentro.Application/
│   ├── Common/
│   │   ├── Interfaces/      # ICommand, IQuery, ICachedQuery, IAppDbContext, IUserContext, ICacheService, etc.
│   │   ├── Behaviors/       # MediatR pipeline behaviors
│   │   ├── Email/           # EmailTemplates (Welcome, MembershipAdded, MembershipRemoved)
│   │   ├── Errors/          # CommonErrors
│   │   └── Models/          # Result<T>, Error, PagedResult<T>
│   └── Features/
│       ├── Me/ | Contacts/ | Activities/ | Webhooks/ | Organizations/
│       └── [Feature]/
│           ├── Commands/    # Handler + outbox event handlers (co-located)
│           ├── Queries/
│           └── [Feature]Errors.cs
├── Propentro.Domain/
│   ├── Common/              # Entity, AggregateRoot, OrganizationEntity, DomainEvent, PublicIdGenerator
│   └── Entities/
│       ├── Identity/        # User, Organization, OrganizationUser, OrganizationUserRole, Role, RolePermission
│       │   └── Events/      # MembershipCreatedEvent, MembershipRemovedEvent
│       ├── Contacts/Events/ # ContactCreatedEvent, ContactUpdatedEvent, etc.
│       ├── Properties/Events/
│       └── [Other entities...]
├── Propentro.Infrastructure/
│   ├── Persistence/AppDbContext.cs   # Global filters + domain event collection
│   ├── Persistence/Configurations/
│   ├── Authorization/       # PermissionService, handlers
│   ├── Caching/             # RedisCacheService, MemoryCacheService
│   ├── Outbox/              # ProcessOutboxMessagesJob
│   └── DependencyInjection.cs
└── Propentro.Contracts/
    └── [Feature]/           # {Verb}{Entity}Request, {Entity}Response, {Entity}ListItemResponse

tests/
├── Propentro.Domain.Tests/
├── Propentro.Application.Tests/
└── Propentro.Api.Tests/
```

## Development Commands

```bash
dotnet build Propentro.slnx
dotnet test Propentro.slnx
dotnet run --project src/Propentro.Api/Propentro.Api.csproj
dotnet ef migrations add MigrationName --project src/Propentro.Infrastructure --startup-project src/Propentro.Api
dotnet ef database update --project src/Propentro.Infrastructure --startup-project src/Propentro.Api
```

**`TreatWarningsAsErrors` is globally enabled — all warnings are build errors.**

## Middleware Pipeline Order

```
ExceptionHandler → Serilog → [Scalar/OpenApi dev] → CORS → RateLimiter
→ Authentication → UserContextMiddleware → Authorization → Endpoints
```

- `UserContextMiddleware` runs AFTER Authentication, BEFORE Authorization — resolves Clerk IDs to internal IDs, enriches Serilog context.
- `ActiveUserAuthorizationHandler` depends on `UserContextMiddleware`.
- Rate limiting: fixed window, 100 req/min.
- Health check at `/health` (anonymous). Scalar at `/scalar/v1` (dev only).

## Key Patterns & Rules

### CQRS — Commands

- Commands implement `ICommand<TResponse>`, handlers implement `ICommandHandler`.
- **Handlers MUST NOT call `SaveChangesAsync`** — `UnitOfWorkBehavior` handles it.
- `PublicId` is generated client-side, available immediately after construction.
- Return `Result<T>` or `Result`. Implicit conversion from Error or value works.

```csharp
public record CreateUnitCommand(string PropertyId, string UnitNumber, UnitType Type) : ICommand<string>;

public class CreateUnitHandler(IAppDbContext db, IUserContext user) : ICommandHandler<CreateUnitCommand, string>
{
    public async Task<Result<string>> Handle(CreateUnitCommand request, CancellationToken ct)
    {
        var property = await db.Properties.FirstOrDefaultAsync(p => p.PublicId == request.PropertyId, ct);
        if (property is null) return UnitErrors.PropertyNotFound;
        var unit = new Unit(property.Id, request.UnitNumber, request.Type);
        db.Units.Add(unit);
        return unit.PublicId;  // Do NOT call SaveChangesAsync
    }
}
```

### CQRS — Queries

- Queries use Dapper. Always `await using` + `OpenAsync`. Always filter by `organization_id`.
- `Dapper MatchNamesWithUnderscores = true` → snake_case columns → PascalCase properties automatically.

```csharp
public record GetUnitByIdQuery(string Id) : IQuery<UnitDetailDto>;

// Handler:
await using var connection = db.CreateConnection();
await connection.OpenAsync(ct);
const string sql = """
    SELECT u.public_id, u.unit_number FROM units u
    WHERE u.public_id = @Id AND u.organization_id = @OrganizationId AND u.is_active = true
    """;
var unit = await connection.QueryFirstOrDefaultAsync<UnitDetailDto>(sql, new { request.Id, user.OrganizationId });
return unit is null ? UnitErrors.NotFound : unit;
```

### Cached Queries

Cache keys are **auto-prefixed** with `org:{OrganizationId}:` by `QueryCachingBehavior`. Do NOT include `OrganizationId` in `CacheKey`.

```csharp
public record GetUnitsQuery(int Page, int PageSize) : IQuery<PagedResult<UnitDto>>, ICachedQuery<PagedResult<UnitDto>>
{
    public string CacheKey => $"units:{Page}:{PageSize}";  // Final: "org:{OrgId}:units:1:20"
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

**MediatR pipeline order:** `Logging → QueryCaching → Validation → UnitOfWork → Handler`
Cache hits skip validation entirely.

### Result Pattern

Error code format: `"Entity.ErrorName"` (dotted, PascalCase).

```csharp
public static class UnitErrors
{
    public static readonly Error NotFound = new("Unit.NotFound", "Unit not found", ErrorType.NotFound);
    public static Error NotFoundById(string id) => new("Unit.NotFound", $"Unit '{id}' not found", ErrorType.NotFound);
}
```

### Domain Entity Pattern

**Entity hierarchy:**
- `Entity` → `Id` (Guid v7) + `PublicId` (prefixed hex)
- `OrganizationEntity : Entity` → adds `OrganizationId`, audit fields, soft delete
- `AggregateRoot : OrganizationEntity` → adds `RaiseDomainEvent`

**Entity prefixes:** `prp`, `unt`, `cnt`, `act`, `adr`, `doc`, `docv`, `ntf`, `pcm`, `ucm`, `usr`, `org`, `rol`, `sadm`

Lookup entities (Country, Currency, Permission, AmenityType, etc.) have no PublicId prefix.

```csharp
public class Unit : AggregateRoot
{
    private Unit() { }  // EF Core only
    public Unit(Guid propertyId, string name, string unitType) : base("unt") { ... }
}
```

### API Endpoint Pattern

```csharp
public static class UnitEndpoints
{
    public static void MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/units").WithTags("Units").RequireAuthorization();
        group.MapGet("/{id}", GetUnitById)
            .WithName("GetUnitById")
            .WithMetadata(new RequirePermissionAttribute("units:read"))
            .Produces<UnitDetailDto>()
            .ProducesProblem(404);
        group.MapPost("/", CreateUnit)
            .WithMetadata(new RequirePermissionAttribute("units:write"))
            .Produces<string>(201)
            .ProducesProblem(400);
    }
    private static async Task<IResult> CreateUnit(CreateUnitRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new CreateUnitCommand(request.PropertyId, request.UnitNumber, request.Type), ct);
        return result.ToCreatedResult($"/api/v1/units/{result.Value}");
    }
}
```

**All endpoints must be registered in `EndpointExtensions.MapApiEndpoints()`.**

### PublicId Validation

Validate format before hitting DB using `[GeneratedRegex]`:

```csharp
[GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
private static partial Regex ContactIdRegex();
// In validator: RuleFor(x => x.Id).NotEmpty().Must(id => ContactIdRegex().IsMatch(id))
```

### Pagination Validation

All paginated queries must enforce: `Page >= 1`, `PageSize` 1–100. (page=0 → invalid SQL OFFSET → 500.)

### Audit Columns

`AppDbContext.SaveChangesAsync` auto-sets: Added → `CreatedAt`, `CreatedById`, `OrganizationId`; Modified → `UpdatedAt`, `UpdatedById`. **Do NOT set these in handlers.** Only set fields NOT in `IAuditableEntity` (e.g., `ArchivedAt`).

### Timestamps

**Always `DateTimeOffset`** for timestamps (matches PostgreSQL TIMESTAMPTZ). `DateOnly` for date-only fields. NEVER `DateTime`.

### Bulk Operations

URL: `POST /v1/{entities}/bulk-{action}`. Body: `{ "ids": [...] }`. Max 50 items (validated). Load all in one query, loop, single transaction. Skip already-in-target-state silently.

### Logging

Use Serilog static `Log` class — **never inject `ILogger`**. Use structured `{PropertyName}` placeholders, not string interpolation. `@` for complex objects.

### Database Conventions

- Tables: `snake_case` plural. Columns: `snake_case`.
- All entities: `id` (Guid v7, internal) + `public_id` (prefixed hex, user-facing).
- Org-scoped: `organization_id`. Soft delete: `is_active`. Audit: `created_at`, `created_by_id`, `updated_at`, `updated_by_id`.

### Pagination Pattern

Paginated queries return `PagedResult<{Entity}ListItemResponse>`. Non-paginated (small/bounded lists) return `IReadOnlyList<{Entity}Response>`. The list item type lives in `Contracts/` as a non-positional `init` record (Dapper-safe). Do not embed pagination fields manually.

```csharp
// Query
public sealed record GetContactsQuery(int Page, int PageSize) : IQuery<PagedResult<ContactListItemResponse>>;

// Handler
return new PagedResult<ContactListItemResponse>(items, total, request.Page, request.PageSize);

// Endpoint
.Produces<PagedResult<ContactListItemResponse>>()
```

`Contracts/` response types **cannot** reference `PagedResult<T>` (Application layer). The `{Entity}ListItemResponse` is the item type; `PagedResult<T>` wraps it in Application/Api layers only.

### Dapper Result Types

**Never use positional records as Dapper targets** (constructor matching breaks with nullables).
**Use non-positional `sealed record` with `init` properties** — Dapper sets `init` via reflection at runtime.

```csharp
// In Contracts/ — usable as both API response and Dapper target
public sealed record UnitResponse
{
    public string PublicId { get; init; } = null!;
    public string UnitNumber { get; init; } = null!;
}
// In handler — no intermediate mapping needed:
var result = await connection.QueryFirstOrDefaultAsync<UnitResponse>(sql, params);
```

| Type | Dapper-safe? | Use for |
|------|-------------|---------|
| Positional record | No | Commands, queries, domain events |
| Non-positional record + `init` | Yes | Contracts/ response types (preferred) |

## Multi-Tenancy

- **EF Core**: global query filters auto-scope by `OrganizationId` — no manual filter needed.
- **Dapper**: must manually add `WHERE organization_id = @OrganizationId`.
- **Cache keys**: auto-prefixed with `org:{OrganizationId}:` by `QueryCachingBehavior`.
- When adding a new entity: add `HasQueryFilter` in `AppDbContext.ApplyGlobalFilters()`.

## Caching

- Redis when `ConnectionStrings:Redis` is set; MemoryCache fallback.
- Both implement per-key `SemaphoreSlim` locking in `GetOrCreateAsync` (stampede protection).
- **Always use `GetOrCreateAsync`**, never manual get-then-set.
- Permission cache: 5 min per user/org. Invalidation:
  - `InvalidatePermissionsCacheAsync(userId, orgId)` — single user
  - `InvalidateOrganizationPermissionsCacheAsync(orgId)` — all users in org
- Cache invalidation is a direct call in handlers (`await cache.RemoveByPatternAsync(...)`). Do NOT use domain events for cache invalidation.

## Side-Effect Decision Guide

| Concern | Mechanism | Transaction |
|---------|-----------|-------------|
| Activity log | Direct write in handler | Same transaction (atomic) |
| Cache invalidation | Direct call in handler | Post-save, best-effort |
| Email / webhook / push | Domain event → Outbox | Async, guaranteed delivery |

## Domain Events & Outbox

Events are **exclusively async** via outbox. No synchronous MediatR dispatch.

**Flow:** `entity.MarkAs{Action}(orgId)` → `RaiseDomainEvent(new XEvent(...))` → `SaveChangesAsync` in UnitOfWork → `AppDbContext` serializes to `outbox_messages` → `ProcessOutboxMessagesJob` polls and dispatches.

**Event type format:** `{aggregate}.{action}` (e.g. `contact.created`). Defined as `override string EventType` on each event record.

**Event location:** `Domain/Entities/{Feature}/Events/` — NOT a top-level `Domain/Events/`.

```csharp
// Domain event
public record ContactCreatedEvent(Guid ContactId, Guid OrganizationId) : DomainEvent(ContactId, OrganizationId)
{
    public override string EventType => "contact.created";
}

// Entity method
public void MarkAsCreated(Guid orgId) => RaiseDomainEvent(new ContactCreatedEvent(Id, orgId));

// In handler — after adding to DbContext
dbContext.Contacts.Add(contact);
contact.MarkAsCreated(currentUser.OrganizationId);
```

**Current event types:**
`contact.created/updated/archived/unarchived/deleted`, `property.created/updated`, `membership.created/removed`

### Outbox Job (ProcessOutboxMessagesJob)

- `[DisallowConcurrentExecution]` Quartz job
- Selects `pending` messages (`next_retry_at IS NULL OR <= now()`), `FOR UPDATE SKIP LOCKED`
- Each message processed in its own mini-transaction
- Success → `completed`; Failure → exponential backoff (`2^retryCount` sec), `retry_count++`; Max retries → `failed`

### Outbox Event Handlers

```csharp
public sealed class ContactArchivedEventHandler : IOutboxEventHandler
{
    public string EventType => "contact.archived";
    public Task HandleAsync(string payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<ContactArchivedEvent>(payload, OutboxJsonOptions.Instance);
        // ... send email, notify external systems
        return Task.CompletedTask;
    }
}
```

- Use `OutboxJsonOptions.Instance` (camelCase) — matches AppDbContext serialization.
- Co-locate handlers with their command, not in a separate folder.
- Unhandled event types → logged as warning, marked completed.

**Current handlers:** `ContactCreated/Updated/Archived/Unarchived/DeletedEventHandler` (Features/Contacts/Commands/), `MembershipCreated/RemovedEventHandler` (Features/Webhooks/Commands/)

## Email

Via `IEmailService` → `ResendEmailService`. Config: `Email:ApiKey`, `Email:FromAddress`. If API key missing → log warning, return success (graceful degradation). Templates: `WelcomeEmail`, `MembershipAddedEmail`, `MembershipRemovedEmail`.

## HTTP Resilience

**Always use custom `AddStandardResilience()`** from `Infrastructure/Resilience/ResiliencePolicies.cs`. **Never `AddStandardResilienceHandler()` (Microsoft's built-in).**

| Strategy | Config |
|----------|--------|
| Total Timeout (outer) | 30s for entire retry cycle |
| Retry | 3 attempts, exponential backoff 500ms + jitter; 5xx/HttpRequestException/TimeoutException |
| Circuit Breaker | Opens at 50% failure over 30s window (min 10 req), stays open 30s |
| Per-attempt Timeout (inner) | 10s |

**Graceful degradation:** if API key/secret missing → log warning, return `Result.Success()`.

## Blob Storage

`IBlobStorageService` → `BlobStorageService`. Config: `BlobStorage` section (`BucketName`, `AccessKey`, `SecretKey`, `ServiceUrl`). Operations: `UploadAsync`, `DownloadAsync`, `DeleteAsync`, `GetPresignedUrlAsync`, `ExistsAsync`.

## Webhooks (Clerk)

- `POST /v1/webhooks/clerk` — anonymous, no auth.
- Svix HMAC-SHA256 verification + replay prevention.
- Idempotency: in-memory store, `svix-id` key, 7-day TTL, 10K max entries.
- On failure → log, remove idempotency entry, return 500 (Clerk retries).
- Handlers use `IgnoreQueryFilters()` (outside tenant context), are idempotent, return `Result.Success()` even if not found.

| Clerk Event | Command |
|-------------|---------|
| `user.created/updated` | `SyncUserCommand` |
| `user.deleted` | `DeactivateUserCommand` |
| `organization.created/updated` | `SyncOrganizationCommand` |
| `organization.deleted` | `DeactivateOrganizationCommand` |
| `organizationMembership.created/updated` | `SyncMembershipCommand` |
| `organizationMembership.deleted` | `RemoveMembershipCommand` |

## Code Style

- 4 spaces, LF, file-scoped namespaces, `_camelCase` private fields
- `var` when type is apparent; explicit for built-ins
- Primary constructors preferred for simple DI
- Async methods → `Async` suffix; interfaces → `I` prefix
- Analysis level: `latest-recommended`; suppressed rules: `CA1515`, `CA1062`, `CA1848`, `CA2007`, `CA1034`, `CA1711`, `CA1716`, `CA1724`, `CA1054/55/56`, `IDE0058`

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Command | `{Verb}{Entity}Command` | `CreateUnitCommand` |
| Query | `Get{Entity}(ById/s)Query` | `GetUnitByIdQuery` |
| Handler | `{Command/Query}Handler` | `CreateUnitHandler` |
| Validator | `{Command}Validator` | `CreateUnitValidator` |
| Response | `{Entity}Response` / `{Entity}ListItemResponse` | `ContactResponse` |
| Request | `{Verb}{Entity}Request` | `CreateContactRequest` |
| Errors | `{Entity}Errors` | `UnitErrors` |
| Domain event | `{Entity}{Action}Event` | `ContactCreatedEvent` |
| Outbox handler | `{Entity}{Action}EventHandler` | `ContactCreatedEventHandler` |

## Common Task Checklists

### Adding a New Entity
1. Create entity in `Domain/Entities/` extending `AggregateRoot` with `base("prefix")`
2. Add `Events/` folder + `MarkAs{Action}` methods
3. Add `DbSet` + `HasQueryFilter` in `AppDbContext`
4. Create EF configuration in `Infrastructure/Persistence/Configurations/`
5. Create Request/Response types in `Contracts/[Feature]/`
6. Create Commands/Queries in `Application/Features/[Feature]/`
7. Create Endpoints + register in `EndpointExtensions.MapApiEndpoints()`
8. `dotnet ef migrations add Add{Entity}`

### Adding a New Command
1. `{Verb}{Entity}Command.cs` record → `ICommand<TResponse>`
2. Handler — do NOT call `SaveChangesAsync`
3. Validator with FluentValidation
4. Request type in `Contracts/[Feature]/`
5. Map endpoint in `Api/Endpoints/{Entity}Endpoints.cs`
6. Add activity log write (if timeline-visible)
7. Add cache invalidation (if cached queries exist)
8. Add domain event + outbox handler (if async side-effects needed)

### Adding a New Query
1. `Get{Entity}(ById/s)Query.cs` → `IQuery<TResponse>`
2. Handler using Dapper (`await using` + `OpenAsync`)
3. Response type in `Contracts/[Feature]/` (non-positional record with `init`)
4. Map endpoint

## Important Files

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Global build settings, TreatWarningsAsErrors, AnalysisLevel |
| `Directory.Packages.props` | Centralized NuGet versions |
| `global.json` | SDK pinning (10.0.102) |
| `.editorconfig` | Code style + CA suppressions |
| `Application/DependencyInjection.cs` | MediatR + validator registration |
| `Application/Common/Interfaces/Messaging/OutboxJsonOptions.cs` | camelCase JSON for outbox |
| `Application/Common/Email/EmailTemplates.cs` | Email templates |
| `Application/Common/Errors/CommonErrors.cs` | Shared errors |
| `Infrastructure/Persistence/AppDbContext.cs` | EF Core + global filters + domain events |
| `Infrastructure/Outbox/ProcessOutboxMessagesJob.cs` | Quartz outbox processor |
| `Domain/Common/PublicIdGenerator.cs` | Client-side PublicId generation |
| `Api/Program.cs` | Bootstrap + middleware pipeline |
| `Api/Endpoints/EndpointExtensions.cs` | Central endpoint registration |

## Reference: /me Endpoint

The `/me` endpoint is the canonical reference. Files: `Api/Endpoints/MeEndpoints.cs`, `Application/Features/Me/Queries/GetMe/`, `Application/Features/Me/MeErrors.cs`, `Contracts/Me/MeDto.cs`.

- No `RequirePermission` — every authenticated user can access their own profile.
- Handler uses Dapper with sequential queries on same connection.
- Error codes: `Me.NotAuthenticated` format.
- Response types: non-positional records with `init`.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Org filter not applied | Ensure entity implements `IOrganizationScoped` |
| Domain events not publishing | Verify `SaveChangesAsync` called by UnitOfWork |
| Dapper mapping fails | Verify `MatchNamesWithUnderscores = true` in Program.cs |
| Validation not running | Ensure validator in same assembly as command |
| Build fails with warnings | `TreatWarningsAsErrors` — fix all warnings |
| Analyzer forces public→internal | Check `CA1515` suppressed in `.editorconfig` |
| Nested `bin/Debug` paths | `dotnet clean` + delete `bin/obj` |

**Debug tips:** `LoggingBehavior` logs all requests with timing. User context enriched via `UserContextMiddleware`. Trace ID in error responses. Use `ASPNETCORE_ENVIRONMENT=Development` for Scalar docs at `/scalar/v1`.
