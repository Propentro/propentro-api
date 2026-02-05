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
| Logging | Serilog with OpenTelemetry |
| Testing | xUnit, FluentAssertions, NSubstitute, Testcontainers |
| Caching | StackExchange.Redis (optional), MemoryCache |

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
│   ├── Common/                    # Entity, AggregateRoot, ValueObject bases
│   ├── Entities/                  # Domain entities (Property, Unit, Lease, etc.)
│   ├── ValueObjects/              # Money, DateRange
│   ├── Enums/                     # LeaseStatus, UnitType, etc.
│   └── Events/                    # Domain events
│
├── Propentro.Infrastructure/      # External concerns
│   ├── Persistence/
│   │   ├── AppDbContext.cs        # EF Core context with filters
│   │   └── Configurations/        # Entity type configurations
│   ├── Services/                  # CurrentUser, Permission, DateTime, BlobStorage
│   ├── Authorization/             # Permission-based auth handlers
│   ├── Caching/                   # ICacheService implementations
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

## Key Patterns & Conventions

### CQRS Command Pattern

Commands modify state and return `Result` or `Result<T>`:

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
    ICurrentUser currentUser,
    IDateTimeProvider dateTime
) : ICommandHandler<CreateUnitCommand, string>
{
    public async Task<Result<string>> Handle(
        CreateUnitCommand request,
        CancellationToken cancellationToken)
    {
        // Validation
        var property = await dbContext.Properties
            .FirstOrDefaultAsync(p => p.PublicId == request.PropertyId, cancellationToken);

        if (property is null)
        {
            return UnitErrors.PropertyNotFound;
        }

        // Create entity
        var unit = new Unit(property.Id, request.UnitNumber, request.Type);

        dbContext.Units.Add(unit);
        // Note: SaveChangesAsync is handled by UnitOfWorkBehavior

        return unit.PublicId;
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

Queries read data using Dapper for performance:

```csharp
// Query definition
public record GetUnitByIdQuery(string Id) : IQuery<UnitDetailDto>;

// Query handler (uses Dapper)
public class GetUnitByIdHandler(
    IDbConnectionFactory connectionFactory,
    ICurrentUser currentUser
) : IQueryHandler<GetUnitByIdQuery, UnitDetailDto>
{
    public async Task<Result<UnitDetailDto>> Handle(
        GetUnitByIdQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();

        const string sql = """
            SELECT u.public_id, u.unit_number, u.type, ...
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

```csharp
public record GetUnitsQuery(int Page, int PageSize)
    : IQuery<PagedResult<UnitDto>>, ICachedQuery<PagedResult<UnitDto>>
{
    public string CacheKey => $"units-{Page}-{PageSize}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

### Result Pattern

Never throw exceptions for business errors:

```csharp
// Error definitions (per feature)
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

    public static readonly Error PropertyNotFound = new(
        "Unit.PropertyNotFound",
        "Associated property not found",
        ErrorType.NotFound);
}

// Usage in handlers
if (unit is null)
{
    return UnitErrors.NotFoundById(request.Id);
}

// Success returns
return Result.Success(unit);
return unit.PublicId;  // Implicit conversion
```

### Domain Entity Pattern

```csharp
public class Unit : OrganizationAggregateRoot
{
    public Guid PropertyId { get; private set; }
    public string UnitNumber { get; private set; } = string.Empty;
    public UnitType Type { get; private set; }
    public UnitStatus Status { get; private set; }

    private Unit() { }  // EF Core

    public Unit(Guid propertyId, string unitNumber, UnitType type)
    {
        PropertyId = propertyId;
        UnitNumber = unitNumber;
        Type = type;
        Status = UnitStatus.Vacant;

        RaiseDomainEvent(new UnitCreatedEvent(Id, unitNumber));
    }

    public void UpdateDetails(string unitNumber, UnitType type)
    {
        UnitNumber = unitNumber;
        Type = type;
    }
}
```

### API Endpoint Pattern

```csharp
public static class UnitEndpoints
{
    public static void MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/units")
            .WithTags("Units")
            .RequireAuthorization();

        group.MapGet("/", GetUnits)
            .WithName("GetUnits")
            .WithSummary("Get paginated list of units")
            .WithMetadata(new RequirePermissionAttribute("units:read"))
            .Produces<PagedResult<UnitDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{id}", GetUnitById)
            .WithName("GetUnitById")
            .WithMetadata(new RequirePermissionAttribute("units:read"))
            .Produces<UnitDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUnit)
            .WithName("CreateUnit")
            .WithMetadata(new RequirePermissionAttribute("units:write"))
            .Produces<string>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetUnits(
        [AsParameters] GetUnitsQuery query,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return result.ToResult();
    }

    private static async Task<IResult> CreateUnit(
        CreateUnitRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateUnitCommand(
            request.PropertyId,
            request.UnitNumber,
            request.Type);

        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedResult($"/api/v1/units/{result.Value}");
    }
}
```

## Code Style Conventions

### C# Conventions (from .editorconfig)

- **Indentation**: 4 spaces
- **Line endings**: LF
- **Namespaces**: File-scoped (`namespace Propentro.Domain.Entities;`)
- **Braces**: Required (`csharp_prefer_braces = true`)
- **var usage**: Use when type is apparent, explicit types for built-ins
- **Private fields**: `_camelCase` prefix
- **Async methods**: Must end with `Async` suffix
- **Interfaces**: Must start with `I` prefix
- **Primary constructors**: Preferred for simple DI

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
// ✅ CORRECT - Use static Log class
using Serilog;

public class MyHandler(IAppDbContext db) : ICommandHandler<MyCommand>
{
    public async Task<Result> Handle(MyCommand request, CancellationToken ct)
    {
        Log.Information("Processing {RequestName}", nameof(MyCommand));
        Log.Debug("Request details: {@Request}", request);

        try
        {
            // ... logic
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process {RequestName}", nameof(MyCommand));
            throw;
        }
    }
}

// ❌ WRONG - Don't inject ILogger
public class MyHandler(IAppDbContext db, ILogger<MyHandler> logger)  // NO!
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
Request → Logging → Validation → UnitOfWork → Handler
                                     ↓
                          BeginTransaction
                                     ↓
                              Handler runs (calls SaveChangesAsync)
                                     ↓
                    ┌─ Success → Commit
                    ├─ Failure → Rollback
                    └─ Exception → Rollback + throw
```

**Important:**
- Handler **MUST** call `SaveChangesAsync` to persist changes
- UnitOfWorkBehavior only manages transaction (Begin/Commit/Rollback)
- `PublicId` is generated by database, available after SaveChanges
- Return `Result.Failure()` for business errors - triggers rollback

```csharp
public async Task<Result<string>> Handle(CreateUnitCommand request, CancellationToken ct)
{
    var unit = new Unit(request.PropertyId, request.UnitNumber);
    db.Units.Add(unit);

    await db.SaveChangesAsync(ct);  // Required - PublicId is set by database

    return unit.PublicId;  // Now available
}
```

### Database Conventions

- Table names: `snake_case` plural (`units`, `properties`)
- Column names: `snake_case` (`unit_number`, `property_id`)
- All entities have `public_id` (user-facing) and `id` (internal Guid)
- Organization-scoped entities have `organization_id`
- Soft delete via `is_active` boolean
- Audit fields: `created_at`, `created_by_id`, `updated_at`, `updated_by_id`

## Multi-Tenancy

The application uses implicit organization-scoped multi-tenancy:

1. **Current user** extracted from JWT claims via `ICurrentUser`
2. **Global query filters** automatically scope all `IOrganizationScoped` entities
3. **SaveChangesAsync** automatically sets `OrganizationId` on new entities
4. **Dapper queries** must include `organization_id` filter manually

```csharp
// EF Core - automatic filtering
var units = await dbContext.Units.ToListAsync();  // Already filtered

// Dapper - manual filtering required
const string sql = """
    SELECT * FROM units
    WHERE organization_id = @OrganizationId
      AND is_active = true
    """;
```

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
        _handler = new CreateUnitHandler(_dbContext, _currentUser, new DateTimeProvider());
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

    [Fact]
    public async Task Handle_WhenPropertyNotFound_ShouldReturnError()
    {
        // Arrange
        _dbContext.Properties.Returns(Array.Empty<Property>().AsQueryable());

        var command = new CreateUnitCommand("invalid-id", "101", UnitType.Apartment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UnitErrors.PropertyNotFound);
    }
}
```

### Integration Tests

```csharp
public class UnitIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private AppDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _dbContext = new AppDbContext(options, ...);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPublishDomainEvents()
    {
        // Test with real database
    }
}
```

## Common Tasks

### Adding a New Entity

1. Create entity in `Domain/Entities/`
2. Add `DbSet` in `AppDbContext`
3. Create configuration in `Infrastructure/Persistence/Configurations/`
4. Create DTOs in `Contracts/[Feature]/`
5. Create Commands/Queries in `Application/[Feature]/`
6. Create Endpoints in `Api/Endpoints/`
7. Add migration: `dotnet ef migrations add Add{Entity}`

### Adding a New Command

1. Create `{Verb}{Entity}Command.cs` record implementing `ICommand<TResponse>`
2. Create `{Verb}{Entity}Handler.cs` implementing `ICommandHandler`
3. Create `{Verb}{Entity}Validator.cs` with FluentValidation rules
4. Add request DTO to Contracts if needed
5. Map endpoint in `Api/Endpoints/{Entity}Endpoints.cs`

### Adding a New Query

1. Create `Get{Entity}(ById/s)Query.cs` implementing `IQuery<TResponse>`
2. Create handler using Dapper for reads
3. Add response DTO to Contracts
4. Map endpoint in `Api/Endpoints/{Entity}Endpoints.cs`

## Important Files Reference

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Global build settings, TFM, nullable, analyzers |
| `Directory.Packages.props` | Centralized NuGet package versions |
| `global.json` | .NET SDK version pinning (10.0.102) |
| `.editorconfig` | Code style rules and naming conventions |
| `Application/DependencyInjection.cs` | MediatR and validator registration |
| `Infrastructure/DependencyInjection.cs` | DB, auth, services registration |
| `Infrastructure/Persistence/AppDbContext.cs` | EF Core context with filters and events |
| `Api/Program.cs` | Application bootstrap and middleware |

## Troubleshooting

### Common Issues

1. **Organization filter not applied**: Ensure entity implements `IOrganizationScoped`
2. **Domain events not publishing**: Check `SaveChangesAsync` is called, not `SaveChanges`
3. **Dapper mapping fails**: Verify `MatchNamesWithUnderscores = true` is set
4. **Validation not running**: Ensure validator is in same assembly as command
5. **Permission denied**: Check user has required permission and cache hasn't expired

### Debug Tips

- All requests logged via `LoggingBehavior` with timing
- User context enriched in logs via `UserContextMiddleware`
- Trace ID available in error responses for debugging
- Use `ASPNETCORE_ENVIRONMENT=Development` for detailed errors
