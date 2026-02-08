# CLAUDE.md - AI Assistant Guide for Propentro API

This document provides comprehensive guidance for AI assistants working with the Propentro API codebase.

## Project Overview

Propentro is a property management API built with **.NET 10.0** following **Clean Architecture** principles. It provides multi-tenant property, unit, lease, and party management with CQRS pattern implementation.

### Technology Stack

| Category | Technologies |
|----------|-------------|
| Framework | .NET 10.0, ASP.NET Core |
| Database | PostgreSQL, EF Core 10.0.2, Dapper |
| Patterns | CQRS, MediatR 14.0, Result Pattern, DDD, UnitOfWork |
| Validation | FluentValidation 12.1 |
| Auth | Clerk JWT (Bearer tokens) |
| Logging | Serilog (static `Log` class) |
| Testing | xUnit, FluentAssertions, NSubstitute, Testcontainers |
| Caching | Redis (default), MemoryCache (fallback) via ICacheService abstraction |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                       Propentro.Api                             │
│                  (Endpoints, Middleware)                        │
└─────────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐
│   Application   │  │  Infrastructure │  │     Contracts       │
│ (CQRS Handlers) │  │ (DB, Services)  │  │ (DTOs, Requests)    │
└─────────────────┘  └─────────────────┘  └─────────────────────┘
         │                    │
         └────────┬───────────┘
                  ▼
         ┌─────────────────┐
         │     Domain      │
         │ (Entities, VOs) │
         └─────────────────┘
```

### Layer Dependencies

- **Api** → Application, Infrastructure, Contracts
- **Application** → Domain, Contracts
- **Infrastructure** → Domain, Application
- **Domain** → No dependencies (pure)
- **Contracts** → No dependencies (DTOs only)

## Project Structure

```
src/
├── Propentro.Api/                 # Web API entry point
│   ├── Endpoints/                 # Feature-based endpoint definitions
│   ├── Middleware/                # GlobalExceptionHandler, UserContextMiddleware
│   ├── Extensions/                # ResultExtensions for IResult conversion
│   └── Program.cs                 # Application bootstrap
│
├── Propentro.Application/         # Business logic layer
│   ├── Common/
│   │   ├── Abstractions/          # ICommand, IQuery, ICachedQuery interfaces
│   │   ├── Behaviors/             # MediatR pipeline behaviors
│   │   └── Models/                # Result<T>, Error, PagedResult<T>
│   ├── [Feature]/
│   │   ├── Commands/              # Create, Update, Delete handlers
│   │   ├── Queries/               # Get, List handlers
│   │   └── [Feature]Errors.cs     # Centralized error definitions
│   └── DependencyInjection.cs
│
├── Propentro.Domain/              # Domain entities and logic
│   ├── Common/                    # Entity, AggregateRoot, ValueObject, PublicIdGenerator
│   ├── Entities/                  # Domain entities (Property, Unit, Lease, etc.)
│   ├── ValueObjects/              # Money, DateRange
│   ├── Enums/                     # LeaseStatus, UnitType, etc.
│   └── Events/                    # Domain events
│
├── Propentro.Infrastructure/      # External concerns
│   ├── Persistence/
│   │   ├── AppDbContext.cs        # EF Core context with global filters
│   │   └── Configurations/        # Entity type configurations
│   ├── Services/                  # CurrentUser, Permission, DateTime, BlobStorage
│   ├── Authorization/             # Permission-based auth handlers
│   ├── Caching/                   # ICacheService (Redis default, MemoryCache fallback)
│   └── DependencyInjection.cs
│
└── Propentro.Contracts/           # Shared DTOs
    └── [Feature]/                 # XyzDto, CreateXyzRequest, etc.

tests/
├── Propentro.Domain.Tests/        # Entity and value object tests
├── Propentro.Application.Tests/   # Handler and validator tests
├── Propentro.Api.Tests/           # Endpoint and middleware tests
└── Propentro.Integration.Tests/   # Full stack tests with Testcontainers
```

## Development Workflows

### Building

```bash
# Build entire solution
dotnet build Propentro.slnx

# Build specific project
dotnet build src/Propentro.Api/Propentro.Api.csproj
```

**Note:** `TreatWarningsAsErrors` is enabled globally. All warnings must be resolved before committing.

### Running Tests

```bash
# Run all tests
dotnet test Propentro.slnx

# Run specific test project
dotnet test tests/Propentro.Domain.Tests/Propentro.Domain.Tests.csproj

# Run with verbosity
dotnet test --verbosity normal
```

### Running the API

```bash
# From the API project directory
dotnet run --project src/Propentro.Api/Propentro.Api.csproj

# With specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Propentro.Api/Propentro.Api.csproj
```

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project src/Propentro.Infrastructure --startup-project src/Propentro.Api

# Update database
dotnet ef database update --project src/Propentro.Infrastructure --startup-project src/Propentro.Api
```

## Middleware Pipeline Order

The middleware pipeline in `Program.cs` follows this exact order:

```
Request → ExceptionHandler → Serilog Logging → CORS → Rate Limiter
        → Authentication → Authorization → UserContextMiddleware
        → Health Checks / API Endpoints → Response
```

**Key points:**
- Authentication runs before `UserContextMiddleware` so JWT claims are available
- `UserContextMiddleware` resolves Clerk external IDs to internal IDs and pushes context to Serilog
- CORS is configured from `Cors:AllowedOrigins` in appsettings
- Rate limiting: fixed window, 100 requests/minute
- Health check at `/health` (anonymous, no auth required)

## Key Patterns & Conventions

### CQRS Command Pattern

Commands modify state and return `Result` or `Result<T>`. **Handlers do NOT call `SaveChangesAsync`** - the `UnitOfWorkBehavior` handles persistence.

```csharp
// Command definition (record)
public record CreateUnitCommand(
    string PropertyId,
    string UnitNumber,
    UnitType Type
) : ICommand<string>;  // Returns PublicId

// Command handler
public class CreateUnitHandler(
    IAppDbContext dbContext,
    ICurrentUser currentUser
) : ICommandHandler<CreateUnitCommand, string>
{
    public async Task<Result<string>> Handle(
        CreateUnitCommand request,
        CancellationToken cancellationToken)
    {
        var property = await dbContext.Properties
            .FirstOrDefaultAsync(p => p.PublicId == request.PropertyId, cancellationToken);

        if (property is null)
            return UnitErrors.PropertyNotFound;

        var unit = new Unit(property.Id, request.UnitNumber, request.Type);

        dbContext.Units.Add(unit);
        // Do NOT call SaveChangesAsync - UnitOfWorkBehavior does this

        return unit.PublicId;  // Available immediately (client-side generated)
    }
}

// Validator
public class CreateUnitValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.UnitNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Type).IsInEnum();
    }
}
```

### CQRS Query Pattern

Queries read data using Dapper for performance. Always use `await using` and `OpenAsync`:

```csharp
// Query definition
public record GetUnitByIdQuery(string Id) : IQuery<UnitDetailDto>;

// Query handler (uses Dapper)
public class GetUnitByIdHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser
) : IQueryHandler<GetUnitByIdQuery, UnitDetailDto>
{
    public async Task<Result<UnitDetailDto>> Handle(
        GetUnitByIdQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT u.public_id, u.unit_number, u.type
            FROM units u
            WHERE u.public_id = @Id
              AND u.organization_id = @OrganizationId
              AND u.is_active = true
            """;

        var unit = await connection.QueryFirstOrDefaultAsync<UnitDetailDto>(
            sql,
            new { request.Id, currentUser.OrganizationId });

        return unit is null
            ? UnitErrors.NotFound
            : unit;
    }
}
```

### Cached Query Pattern

Cache keys are **automatically prefixed** with `org:{OrganizationId}:` by `QueryCachingBehavior` for multi-tenant isolation. Do NOT include `OrganizationId` in `CacheKey` — it is added automatically.

```csharp
public record GetUnitsQuery(int Page, int PageSize)
    : IQuery<PagedResult<UnitDto>>, ICachedQuery<PagedResult<UnitDto>>
{
    // Final key: "org:{OrganizationId}:units:{Page}:{PageSize}" (auto-prefixed)
    public string CacheKey => $"units:{Page}:{PageSize}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

### Result Pattern

Never throw exceptions for business errors:

```csharp
// Error definitions (per feature)
// Code format: "Entity.ErrorName" (dotted, PascalCase)
public static class UnitErrors
{
    public static readonly Error NotFound = new(
        "Unit.NotFound",
        "Unit not found",
        ErrorType.NotFound);

    public static Error NotFoundById(string id) => new(
        "Unit.NotFound",
        $"Unit with ID '{id}' was not found",
        ErrorType.NotFound);
}

// Usage in handlers
if (unit is null)
    return UnitErrors.NotFoundById(request.Id);

// Success returns
return Result.Success(unit);
return unit.PublicId;  // Implicit conversion
```

### Domain Entity Pattern

```csharp
public class Unit : AggregateRoot
{
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = default!;
    public string UnitType { get; set; } = default!;
    public string Status { get; set; } = "available";

    private Unit() { }  // EF Core

    public Unit(Guid propertyId, string name, string unitType) : base("unt")
    {
        // Id (Guid v7) and PublicId (unt_a3f9c12d8e4b) set by Entity base class
        PropertyId = propertyId;
        Name = name;
        UnitType = unitType;
    }
}
```

**Key domain conventions:**
- `Id` (Guid v7) - internal identifier, auto-generated in `Entity(prefix)` constructor
- `PublicId` (prefixed hex: `prp_a3f9c12d8e4b`) - user-facing, generated client-side by `PublicIdGenerator`
- Both IDs are available immediately after construction - no need to wait for database
- Each entity defines its prefix via `base("prefix")`: `prp`, `unt`, `lse`, `pty`, `adr`, `doc`, `usr`, `org`, `rol`
- Parameterless constructor is for EF Core only (no ID generation)

### API Endpoint Pattern

```csharp
public static class UnitEndpoints
{
    public static void MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/units")
            .WithTags("Units")
            .RequireAuthorization();

        group.MapGet("/{id}", GetUnitById)
            .WithName("GetUnitById")
            .WithSummary("Get unit by ID")
            .WithMetadata(new RequirePermissionAttribute("units:read"))
            .Produces<UnitDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUnit)
            .WithName("CreateUnit")
            .WithMetadata(new RequirePermissionAttribute("units:write"))
            .Produces<string>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateUnit(
        CreateUnitRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateUnitCommand(request.PropertyId, request.UnitNumber, request.Type);
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult($"/api/v1/units/{result.Value}");
    }
}
```

**All endpoints must be registered in `EndpointExtensions.MapApiEndpoints()`.**

## Code Style Conventions

### C# Conventions (from .editorconfig)

- **Indentation**: 4 spaces
- **Line endings**: LF
- **Namespaces**: File-scoped (`namespace Propentro.Domain.Entities;`)
- **Braces**: Required for multi-line blocks, optional for single-line (`csharp_prefer_braces = when_multiline`)
- **var usage**: Use when type is apparent, explicit types for built-ins
- **Private fields**: `_camelCase` prefix
- **Async methods**: Must end with `Async` suffix
- **Interfaces**: Must start with `I` prefix
- **Primary constructors**: Preferred for simple DI
- **Warnings as errors**: Enabled globally - all warnings must be resolved
- **Analysis level**: `latest-recommended` (not `latest-all`) - aggressive rules suppressed in `.editorconfig`

### Suppressed Analyzer Rules

The following CA rules are suppressed in `.editorconfig` to avoid false positives:

| Rule | Reason |
|------|--------|
| `CA1515` | "Make public types internal" - impractical for DI-heavy projects |
| `CA1062` | "Validate public method args" - redundant with nullable reference types |
| `CA1848` | "Use LoggerMessage delegates" - project uses Serilog static `Log` |
| `CA2007` | "ConfigureAwait" - not needed in ASP.NET Core |
| `CA1034` | "Nested types not visible" - common for DTOs/result types |
| `CA1716` | "Identifiers match keywords" - `Me` is VB.NET keyword, irrelevant for C# |
| `CA1724` | "Type names match namespaces" - false positives with domain names |
| `CA1054/55/56` | "URI as strings" - simpler for API DTOs |
| `IDE0058` | "Expression value unused" - noisy with fluent APIs |

### File Organization

- One class per file (generally)
- File name matches class name
- Organize by feature, not by type
- Keep related command/query/handler/validator together

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Command | `{Verb}{Entity}Command` | `CreateUnitCommand` |
| Query | `Get{Entity}(ById/s)Query` | `GetUnitByIdQuery` |
| Handler | `{Command/Query}Handler` | `CreateUnitHandler` |
| Validator | `{Command}Validator` | `CreateUnitValidator` |
| DTO | `{Entity}Dto` / `{Entity}DetailDto` | `UnitDto`, `UnitDetailDto` |
| Request | `{Verb}{Entity}Request` | `CreateUnitRequest` |
| Error class | `{Entity}Errors` | `UnitErrors` |

### Logging Conventions

This project uses **Serilog's static `Log` class** instead of injected `ILogger`:

```csharp
using Serilog;

public class MyHandler(IAppDbContext db) : ICommandHandler<MyCommand>
{
    public async Task<Result> Handle(MyCommand request, CancellationToken ct)
    {
        Log.Information("Processing {RequestName}", nameof(MyCommand));
        // ...
    }
}

// Don't inject ILogger - use static Log class
```

**Log Levels:**
- `Log.Debug()` - Detailed diagnostic info (transactions, queries)
- `Log.Information()` - General operational events
- `Log.Warning()` - Unexpected but handled situations
- `Log.Error()` - Errors and exceptions

**Structured Logging:**
- Use `{PropertyName}` placeholders, not string interpolation
- Use `@` for complex object serialization: `{@Request}`

### Transaction Management (UnitOfWork)

Commands are wrapped in database transactions via `UnitOfWorkBehavior`:

```
Request → Logging → QueryCaching → Validation → UnitOfWork → Handler
                        ↓                           ↓
                   Cache hit?                  BeginTransaction
                   ├─ Yes → Return                   ↓
                   └─ No  → Continue           Handler runs
                                                     ↓
                                  ┌─ Success → SaveChangesAsync → Commit
                                  ├─ Failure → Rollback (no save)
                                  └─ Exception → Rollback + throw
```

**Critical rules:**
- Handlers **must NOT** call `SaveChangesAsync` - `UnitOfWorkBehavior` does it automatically
- `UnitOfWorkBehavior` calls `SaveChangesAsync` then `CommitAsync` on success
- On `Result.Failure()`, the transaction is rolled back without saving
- On exception, the transaction is rolled back and the exception rethrows
- `PublicId` is generated client-side, available immediately after entity creation

```csharp
public async Task<Result<string>> Handle(CreateUnitCommand request, CancellationToken ct)
{
    var unit = new Unit(request.PropertyId, request.UnitNumber);
    db.Units.Add(unit);

    // Do NOT call SaveChangesAsync here
    // UnitOfWorkBehavior handles: SaveChangesAsync → Commit

    return unit.PublicId;  // Already available (client-side generated)
}
```

### Database Conventions

- Table names: `snake_case` plural (`units`, `properties`)
- Column names: `snake_case` (`unit_number`, `property_id`)
- All entities have `public_id` (user-facing, prefixed hex e.g. `prp_a3f9c12d8e4b`) and `id` (internal Guid v7)
- Organization-scoped entities have `organization_id`
- Soft delete via `is_active` boolean
- Audit fields: `created_at`, `created_by_id`, `updated_at`, `updated_by_id`

### Dapper Query Conventions

```csharp
// Always use await using + OpenAsync
await using var connection = db.CreateConnection();
await connection.OpenAsync(cancellationToken);

// Always filter by organization_id for tenant isolation
const string sql = """
    SELECT u.public_id, u.unit_number
    FROM units u
    WHERE u.organization_id = @OrganizationId
      AND u.is_active = true
    """;

var result = await connection.QueryAsync<UnitDto>(sql, new { currentUser.OrganizationId });
```

**Important:** Dapper `MatchNamesWithUnderscores = true` is set in `Program.cs`, so `snake_case` DB columns map to `PascalCase` C# properties automatically.

## Multi-Tenancy

The application uses implicit organization-scoped multi-tenancy:

1. **Current user** extracted from JWT claims via `ICurrentUser`
2. **Global query filters** automatically scope all `IOrganizationScoped` entities by referencing `AppDbContext.CurrentOrganizationId` via lambda closures
3. **SaveChangesAsync** automatically sets `OrganizationId` on new entities
4. **Dapper queries** must include `organization_id = @OrganizationId` filter manually

```csharp
// EF Core - automatic filtering via global query filters
var units = await dbContext.Units.ToListAsync();  // Already filtered by org

// Dapper - manual filtering required
const string sql = """
    SELECT * FROM units
    WHERE organization_id = @OrganizationId
      AND is_active = true
    """;
```

**How global filters work:** `AppDbContext` exposes `CurrentOrganizationId` which delegates to `ICurrentUser.OrganizationId`. Each entity has an explicit `HasQueryFilter` call in `ApplyGlobalFilters()` that references `CurrentOrganizationId`. EF Core rebinds this DbContext property per query, ensuring correct tenant isolation. When adding a new entity, add the corresponding `HasQueryFilter` line in `AppDbContext.ApplyGlobalFilters()`.

## Caching

### Cache Provider Selection

The cache provider is configured via `Caching:Provider` in appsettings:

- **Redis** (default for production): Used when `Provider = "Redis"` and `RedisConnectionString` is set. Suitable for multi-instance deployments.
- **Memory** (fallback): Used when `Provider = "Memory"` or Redis connection string is empty. Suitable for development and single-instance deployments.

```json
{
  "Caching": {
    "Provider": "Redis",
    "RedisConnectionString": "localhost:6379",
    "DefaultExpiration": "00:05:00",
    "KeyPrefix": "propentro:"
  }
}
```

### Cache Stampede Protection

Both `MemoryCacheService` and `RedisCacheService` implement **per-key `SemaphoreSlim` locking** in `GetOrCreateAsync` to prevent cache stampede (thundering herd):

```
Thread 1: Cache miss → Acquire lock → DB query → Set cache → Release
Thread 2: Cache miss → Wait for lock → Cache hit (from Thread 1) → Return
Thread 3: Cache miss → Wait for lock → Cache hit (from Thread 1) → Return
```

**Always use `GetOrCreateAsync`** for hot-path caching (e.g., `PermissionService`). Never use manual get-then-set pattern.

### Multi-Tenant Cache Key Enforcement

`QueryCachingBehavior` **automatically prefixes** all `ICachedQuery` cache keys with `org:{OrganizationId}:`. This prevents cross-tenant data leaks. When defining a cached query's `CacheKey`, do NOT include `OrganizationId` — it is added by the behavior.

### Permission Caching

`PermissionService` caches user permissions per organization for 5 minutes using `GetOrCreateAsync` (stampede-protected). Two invalidation levels are available:

| Method | Scope | When to Use |
|--------|-------|-------------|
| `InvalidatePermissionsCacheAsync(userId, orgId)` | Single user | User's role assignment changes |
| `InvalidateOrganizationPermissionsCacheAsync(orgId)` | All users in org | Role or permission definitions change |

### MediatR Pipeline Caching Order

```
Logging → QueryCaching → Validation → UnitOfWork → Handler
```

`QueryCachingBehavior` runs **before** `ValidationBehavior`, so cache hits skip validation entirely for optimal performance.

## Testing Guidelines

### Unit Tests (Domain/Application)

```csharp
public class CreateUnitHandlerTests
{
    private readonly IAppDbContext _dbContext = Substitute.For<IAppDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreateUnitHandler _handler;

    public CreateUnitHandlerTests()
    {
        _handler = new CreateUnitHandler(_dbContext, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPropertyExists_ShouldCreateUnit()
    {
        // Arrange
        var property = new Property("Test Property", PropertyKind.Residential);
        _dbContext.Properties.Returns(new[] { property }.AsQueryable());
        var command = new CreateUnitCommand(property.PublicId, "101", UnitType.Apartment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }
}
```

### Integration Tests

```csharp
public class UnitIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Setup DbContext with connection string
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

## Common Tasks

### Adding a New Entity

1. Create entity in `Domain/Entities/` extending `AggregateRoot` (or `OrganizationEntity`)
2. Add constructor with prefix: `public MyEntity(...) : base("mye") { ... }`
3. Add `DbSet` in `AppDbContext`
4. Add `HasQueryFilter` in `AppDbContext.ApplyGlobalFilters()`
5. Create configuration in `Infrastructure/Persistence/Configurations/`
6. Create DTOs in `Contracts/[Feature]/`
7. Create Commands/Queries in `Application/[Feature]/`
8. Create Endpoints in `Api/Endpoints/`
9. Register endpoints in `EndpointExtensions.MapApiEndpoints()`
10. Add migration: `dotnet ef migrations add Add{Entity}`

### Adding a New Command

1. Create `{Verb}{Entity}Command.cs` record implementing `ICommand<TResponse>`
2. Create `{Verb}{Entity}Handler.cs` implementing `ICommandHandler` - do NOT call `SaveChangesAsync`
3. Create `{Verb}{Entity}Validator.cs` with FluentValidation rules
4. Add request DTO to `Contracts/[Feature]/`
5. Map endpoint in `Api/Endpoints/{Entity}Endpoints.cs`

### Adding a New Query

1. Create `Get{Entity}(ById/s)Query.cs` implementing `IQuery<TResponse>`
2. Create handler using Dapper (`await using` + `OpenAsync` pattern)
3. Add response DTO to `Contracts/[Feature]/`
4. Map endpoint in `Api/Endpoints/{Entity}Endpoints.cs`

## Important Files Reference

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Global build settings, TFM, nullable, TreatWarningsAsErrors, AnalysisLevel |
| `Directory.Packages.props` | Centralized NuGet package versions |
| `global.json` | .NET SDK version pinning (10.0.102) |
| `.editorconfig` | Code style rules and naming conventions |
| `Application/DependencyInjection.cs` | MediatR and validator registration |
| `Infrastructure/DependencyInjection.cs` | DB, auth, caching, services registration |
| `Infrastructure/Persistence/AppDbContext.cs` | EF Core context with global filters and domain events |
| `Domain/Common/PublicIdGenerator.cs` | Client-side prefixed PublicId generation |
| `Api/Program.cs` | Application bootstrap and middleware pipeline |
| `Api/Endpoints/EndpointExtensions.cs` | Central endpoint registration |

## Reference Implementation: /me Endpoint

The `/me` endpoint is the reference implementation. All future endpoints should follow the same patterns.

### Response Shape

```json
{
  "userId": "usr_a3f9c12d8e4b",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "organization": {
    "organizationId": "org_9bc21f0a7d33",
    "name": "Acme Corp",
    "slug": "acme-corp",
    "currencyCode": "EUR",
    "timezone": "Europe/Istanbul"
  },
  "roles": ["admin"],
  "permissions": ["properties:read", "properties:write", "units:read"]
}
```

### Files

| File | Purpose |
|------|---------|
| `Api/Endpoints/MeEndpoints.cs` | Endpoint definition with metadata |
| `Application/Me/Queries/GetMe/GetMeQuery.cs` | Query record |
| `Application/Me/Queries/GetMe/GetMeHandler.cs` | Dapper handler with roles + permissions |
| `Application/Me/MeErrors.cs` | Error definitions |
| `Contracts/Me/MeDto.cs` | Response DTOs |

### Key Patterns Demonstrated

- **No `RequirePermission`** on `/me` - every authenticated user can view their own profile
- **Dapper query** with `await using` + `OpenAsync` + sequential queries on same connection
- **Error codes** follow `Entity.ErrorName` format (e.g., `Me.NotAuthenticated`)
- **DTO** uses positional records, nullable fields where data may not exist
- **Handler** checks `ICurrentUser.IsAuthenticated` before any DB access
- **Endpoint** uses `ProducesProblem()` for error responses, `Produces<T>()` for success

## Troubleshooting

### Common Issues

1. **Organization filter not applied**: Ensure entity implements `IOrganizationScoped`
2. **Domain events not publishing**: Check `SaveChangesAsync` is called (by UnitOfWork, not handler)
3. **Dapper mapping fails**: Verify `MatchNamesWithUnderscores = true` is set in Program.cs
4. **Validation not running**: Ensure validator is in same assembly as command
5. **Permission denied**: Check user has required permission and cache hasn't expired
6. **Build fails with warnings**: `TreatWarningsAsErrors` is enabled - fix all warnings
7. **Analyzer forces public→internal**: Check if `CA1515` is suppressed in `.editorconfig`; if a new aggressive CA rule appears, suppress it there

### Debug Tips

- All requests logged via `LoggingBehavior` with timing
- User context enriched in logs via `UserContextMiddleware`
- Trace ID available in error responses for debugging
- Use `ASPNETCORE_ENVIRONMENT=Development` for detailed errors and Scalar API docs
- Scalar API reference available at `/scalar/v1` in development
