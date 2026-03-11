# CLAUDE.md - Propentro API

Propentro: .NET 10.0 property management API. Clean Architecture, multi-tenant, CQRS with MediatR.

## Tech Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Framework | .NET 10.0, ASP.NET Core | SDK 10.0.102 |
| Database | PostgreSQL, EF Core, Dapper | EF 10.0.2, Dapper 2.1.66 |
| CQRS | MediatR | 14.0.0 |
| Validation | FluentValidation | 12.1.1 |
| Auth | Clerk JWT (Bearer tokens) | - |
| Logging | Serilog (static `Log` class) | 4.3.0 |
| Testing | xUnit, FluentAssertions, NSubstitute, Testcontainers | - |
| Caching | Redis (primary) / MemoryCache (fallback) | StackExchange.Redis |
| Email | Resend via HTTP | - |
| Storage | S3-compatible (Cloudflare R2) via AWSSDK.S3 | 4.0.18.7 |
| Background | Quartz.NET | 3.15.1 |
| Resilience | Polly via Microsoft.Extensions.Http.Resilience | 10.2.0 |
| API Docs | Scalar (dev only) | 2.12.30 |

## Commands

```bash
dotnet build Propentro.slnx
dotnet test Propentro.slnx
dotnet run --project src/Propentro.Api/Propentro.Api.csproj
dotnet ef migrations add MigrationName --project src/Propentro.Infrastructure --startup-project src/Propentro.Api
dotnet ef database update --project src/Propentro.Infrastructure --startup-project src/Propentro.Api
```

`TreatWarningsAsErrors` is globally enabled in `Directory.Build.props`. Every warning is a build error.

## Architecture

### Layer Dependencies

```
Api --> Application + Infrastructure + Contracts
Application --> Domain + Contracts
Infrastructure --> Domain + Application
Domain --> (nothing)
Contracts --> (nothing)
```

DO NOT add a reference from Domain to any other project. DO NOT reference Application types from Contracts.

### Project Structure

```
src/
  Propentro.Api/
    Program.cs                          # Bootstrap, middleware pipeline
    Endpoints/                          # Minimal API endpoint groups
      EndpointExtensions.cs             # Central registration (ALL endpoints go here)
      MeEndpoints.cs
      WebhookEndpoints.cs
      Organizations/
        MembersEndpoints.cs
        RolesEndpoints.cs
        SettingsEndpoints.cs
    Middleware/
      GlobalExceptionHandler.cs         # Catches ValidationException, DbUpdateConcurrencyException, etc.
      UserContextMiddleware.cs          # Resolves Clerk IDs to internal IDs, enriches Serilog
    Extensions/
      ResultExtensions.cs              # Result<T> -> IResult conversion

  Propentro.Application/
    DependencyInjection.cs             # MediatR pipeline + validators
    GlobalUsings.cs                    # Global imports for Serilog, domain entities, interfaces
    Common/
      Behaviors/                       # MediatR pipeline behaviors
        LoggingBehavior.cs
        QueryCachingBehavior.cs
        ValidationBehavior.cs
        UnitOfWorkBehavior.cs
      Interfaces/
        Authentication/                # IUserContext, IUserContextInitializer, IExternalIdResolver, IClerkOrganizationService
        Authorization/                 # IPermissionService
        Caching/                       # ICacheService
        Email/                         # IEmailService, EmailMessage
        Messaging/                     # ICommand, IQuery, ICachedQuery, IOutboxEventHandler, OutboxJsonOptions
        Notifications/                 # INotificationService
        Persistence/                   # IAppDbContext, IDbConnectionFactory
        Storage/                       # IBlobStorageService
        Time/                          # IDateTimeProvider
      Email/
        EmailTemplates.cs              # WelcomeEmail, MembershipAddedEmail, MembershipRemovedEmail
      Errors/
        CommonErrors.cs                # Unauthorized, Forbidden, ValidationFailed, Conflict, NotFound
      Models/
        Error.cs                       # Error record + ErrorType enum
        Result.cs                      # Result, Result<T> with implicit conversions
        PagedResult.cs                 # PagedResult<T> with TotalPages, HasPrevious, HasNext
    Features/
      Me/
        MeErrors.cs
        Queries/GetMe/
      Organizations/
        Members/
          MemberErrors.cs
          Commands/InviteMember/
          Commands/DeactivateMember/
          Commands/ReactivateMember/
          Commands/UpdateMemberRoles/
          Queries/GetOrganizationMembers/
        Roles/
          RoleErrors.cs
          Commands/CreateRole/
          Commands/UpdateRole/
          Commands/DeleteRole/
          Queries/GetOrganizationRoles/
          Permissions/Queries/GetPermissions/
        Settings/
          OrganizationSettingsErrors.cs
          Commands/UpdateOrganizationSettings/
          Queries/GetOrganizationSettings/
      Webhooks/
        Commands/SyncUser/
        Commands/SyncOrganization/
        Commands/DeactivateOrganization/
        Commands/SyncMembership/
          SyncMembershipCommand.cs
          SyncMembershipHandler.cs
          MembershipCreatedEventHandler.cs  # Outbox handler co-located with command

  Propentro.Domain/
    Common/
      Entity.cs                        # Id (Guid v7) + PublicId (prefixed hex)
      OrganizationEntity.cs            # + OrganizationId, audit fields, IsActive
      AggregateRoot.cs                 # + RaiseDomainEvent, ClearDomainEvents
      DomainEvent.cs                   # abstract record with EventType, OccurredAt
      PublicIdGenerator.cs             # Generate(prefix) -> "prefix_12hexchars"
      ValueObject.cs                   # Equality by components
      Interfaces/                      # IAuditableEntity, IHasPublicId, IOrganizationScoped, ISoftDeletable, IDomainEvent
    Entities/
      Identity/                        # User, Organization, OrganizationUser, OrganizationUserRole, Role, RolePermission, Permission, SystemAdministrator
        Events/                        # MembershipCreatedEvent, MembershipRemovedEvent
      Contacts/                        # Contact, ContactRole, ContactDocument
        Events/                        # ContactCreated/Updated/Archived/Unarchived/DeletedEvent
      Properties/                      # Property, PropertyAmenity, PropertyCompliance, PropertyContactRole, PropertyDocument, PropertyComplianceDocument, PropertyPurpose, PropertyType
        Events/                        # PropertyCreatedEvent, PropertyUpdatedEvent
      Units/                           # Unit, UnitAmenity, UnitCompliance, UnitContactRole, UnitDocument, UnitComplianceDocument
      Addresses/                       # Address
      Activities/                      # Activity
      Documents/                       # Document, DocumentVersion
      Notifications/                   # Notification, NotificationPreference
      Tags/                            # Tag, EntityTag
      Lookups/                         # AmenityType, ComplianceType
      Outbox/                          # OutboxMessage
    Enums/                             # ComplianceStatus, DocumentType, FurnishedStatus, RentalMode, UnitStatus, UnitType, UsageType
    ValueObjects/                      # Money, DateRange

  Propentro.Infrastructure/
    DependencyInjection.cs
    GlobalUsings.cs
    Persistence/
      AppDbContext.cs                  # Global filters, audit fields, outbox persistence
      DbConnectionFactory.cs          # NpgsqlConnection for Dapper
      Configurations/                  # One IEntityTypeConfiguration<T> per entity
    Authentication/
      UserContext.cs                   # IUserContext + IUserContextInitializer impl
      ClerkIdResolver.cs              # IExternalIdResolver impl
      ClerkOrganizationService.cs     # IClerkOrganizationService impl
    Authorization/
      RequirePermissionAttribute.cs   # Endpoint metadata
      PermissionAuthorizationHandler.cs
      ActiveUserAuthorizationHandler.cs
      ActiveUserRequirement.cs
      PermissionService.cs            # IPermissionService with cache
      PermissionLogic.cs              # enum All/Any
    Caching/
      RedisCacheService.cs            # ICacheService with SemaphoreSlim stampede protection
      MemoryCacheService.cs           # ICacheService fallback
      CachingOptions.cs
    Outbox/
      ProcessOutboxMessagesJob.cs     # Quartz [DisallowConcurrentExecution] job
      ProcessOutboxMessagesJobSetup.cs
      OutboxOptions.cs
    Email/
      ResendEmailService.cs           # IEmailService with graceful degradation
      EmailOptions.cs
    Storage/
      BlobStorageService.cs           # IBlobStorageService (S3/R2)
      BlobStorageOptions.cs
    Webhooks/
      ClerkWebhookVerifier.cs         # Svix HMAC-SHA256
      WebhookIdempotencyStore.cs      # In-memory, svix-id key
    Resilience/
      ResiliencePolicies.cs           # AddStandardResilience() extension
    Clock/
      DateTimeProvider.cs
    Notifications/
      NotificationService.cs

  Propentro.Contracts/
    Me/
      MeResponse.cs
    Organizations/
      Members/                         # InviteMemberRequest, UpdateMemberRolesRequest, MemberListItemResponse, MemberRoleResponse
      Roles/                           # CreateRoleRequest, UpdateRoleRequest, RoleResponse
        Permissions/                   # PermissionGroupResponse, PermissionItemResponse
      Settings/                        # UpdateOrganizationSettingsRequest, OrganizationSettingsResponse
    Webhooks/
      ClerkWebhookPayload.cs

tests/
  Propentro.Domain.Tests/
  Propentro.Application.Tests/
  Propentro.Api.Tests/
```

## Middleware Pipeline Order

```
ExceptionHandler -> Serilog -> [Scalar/OpenApi dev-only] -> CORS -> RateLimiter
  -> Authentication -> UserContextMiddleware -> Authorization -> Endpoints
```

- `UserContextMiddleware` runs AFTER Authentication, BEFORE Authorization. Resolves Clerk external IDs to internal DB IDs and pushes UserId/OrganizationId/ExternalUserId into Serilog LogContext.
- `ActiveUserAuthorizationHandler` depends on `UserContextMiddleware` having run.
- Rate limiting: fixed window, 100 req/min.
- Health check at `/health` (anonymous). Scalar at `/scalar/v1` (dev only).

## MediatR Pipeline Order

```
Logging -> QueryCaching -> Validation -> UnitOfWork -> Handler
```

Registered in `Application/DependencyInjection.cs`:

```csharp
cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
```

- Cache hits skip validation and handler entirely.
- `UnitOfWorkBehavior` only wraps commands (checks for `ICommand`/`ICommand<T>`), not queries.
- `ValidationBehavior` throws `FluentValidation.ValidationException` on failure (caught by `GlobalExceptionHandler`).

## CQRS Patterns

### Commands

Commands use EF Core for writes. Handlers MUST NOT call `SaveChangesAsync` -- `UnitOfWorkBehavior` handles persistence and transactions.

```csharp
// Command record -- from CreateRoleCommand.cs
public sealed record CreateRoleCommand(string Name, string? Description, string[] PermissionCodes) : ICommand<string>;

// Handler -- from CreateRoleHandler.cs
public sealed class CreateRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<CreateRoleCommand, string>
{
    public async Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // ... validation logic ...
        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            PublicId = Domain.Common.PublicIdGenerator.Generate(Role.PublicIdPrefix),
            OrganizationId = currentUser.OrganizationId,
            Name = request.Name,
            // ...
        };
        dbContext.Roles.Add(role);
        // DO NOT call SaveChangesAsync -- UnitOfWork handles it
        return role.PublicId;
    }
}
```

For commands that return no data, use `ICommand` (not `ICommand<T>`) and `ICommandHandler<TCommand>`:

```csharp
// From InviteMemberCommand.cs
public sealed record InviteMemberCommand(string Email, string RoleId) : ICommand;

// Handler returns Result (not Result<T>)
public sealed class InviteMemberHandler(...) : ICommandHandler<InviteMemberCommand>
{
    public async Task<Result> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        // ...
        return Result.Success();
    }
}
```

### Queries

Queries use Dapper for reads. Always `await using` + `OpenAsync`. Always filter by `organization_id` manually (no EF global filters for Dapper).

```csharp
// Query record -- from GetOrganizationMembersQuery.cs
public sealed record GetOrganizationMembersQuery(int Page, int PageSize, string? Search, string? Status, string? RoleId)
    : IQuery<PagedResult<MemberListItemResponse>>;

// Handler -- from GetOrganizationMembersHandler.cs
public sealed class GetOrganizationMembersHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetOrganizationMembersQuery, PagedResult<MemberListItemResponse>>
{
    public async Task<Result<PagedResult<MemberListItemResponse>>> Handle(...)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Always filter by organization_id in Dapper queries
        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", currentUser.OrganizationId);
        // ...

        const string sql = """
            SELECT ...
            FROM organization_users ou
            JOIN users u ON u.id = ou.user_id
            WHERE ou.organization_id = @OrganizationId
            ...
            """;
    }
}
```

Dapper `MatchNamesWithUnderscores = true` is set in `Program.cs`, so `snake_case` DB columns map to `PascalCase` properties automatically.

### Cached Queries

Implement `ICachedQuery<TResponse>`. Cache keys are auto-prefixed with `org:{OrganizationId}:` by `QueryCachingBehavior`. DO NOT include OrganizationId in `CacheKey`.

```csharp
// From GetOrganizationRolesQuery.cs
public sealed record GetOrganizationRolesQuery : ICachedQuery<IReadOnlyList<RoleResponse>>
{
    public string CacheKey => "roles";                         // Final key: "org:{OrgId}:roles"
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

// From GetOrganizationSettingsQuery.cs
public sealed record GetOrganizationSettingsQuery : ICachedQuery<OrganizationSettingsResponse>
{
    public string CacheKey => "org-settings";                  // Final key: "org:{OrgId}:org-settings"
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

When invalidating cached queries from command handlers, use the full key including org prefix:

```csharp
// From CreateRoleHandler.cs
await cache.RemoveAsync($"org:{currentUser.OrganizationId}:roles", cancellationToken);

// From UpdateOrganizationSettingsHandler.cs
await cache.RemoveAsync($"org:{currentUser.OrganizationId}:org-settings", cancellationToken);
```

## Result Pattern

Every handler returns `Result` or `Result<T>`. Implicit conversion from `Error` or value:

```csharp
// Return error (implicit conversion)
return RoleErrors.NameAlreadyExists;

// Return value (implicit conversion)
return role.PublicId;

// Explicit success with no data
return Result.Success();
```

### Error Definitions

Error code format: `"Entity.ErrorName"` (dotted, PascalCase). One static class per feature.

```csharp
// From RoleErrors.cs
public static class RoleErrors
{
    public static readonly Error NotFound = new(
        "Role.NotFound",
        "Role not found.",
        ErrorType.NotFound);

    public static readonly Error CannotModifySystemRole = new(
        "Role.CannotModifySystemRole",
        "System roles cannot be modified or deleted.",
        ErrorType.Conflict);

    public static readonly Error NameAlreadyExists = new(
        "Role.NameAlreadyExists",
        "A role with this name already exists in the organization.",
        ErrorType.Conflict);
}
```

ErrorType enum: `Failure`, `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`.

`ResultExtensions.cs` maps these to HTTP status codes:
- `NotFound` -> 404
- `Unauthorized` -> 401
- `Forbidden` -> 403
- `Validation` -> 400
- `Conflict` -> 409
- `Failure` -> 400

## Domain Entity Pattern

### Entity Hierarchy

```
Entity                    # Id (Guid v7) + PublicId (prefixed hex)
  OrganizationEntity      # + OrganizationId, CreatedAt/By, UpdatedAt/By, IsActive (soft delete)
    AggregateRoot         # + RaiseDomainEvent(), ClearDomainEvents()
```

- `Entity` -- for entities without org scope (User, Organization, Role, SystemAdministrator).
- `OrganizationEntity` -- for org-scoped entities without domain events.
- `AggregateRoot` -- for org-scoped entities that raise domain events (Contact, Property, Unit).

Some entities like `Role` extend `Entity` directly and manually implement `IAuditableEntity` + `ISoftDeletable` because they have nullable `OrganizationId` (system roles have no org).

### PublicId Prefixes

Current prefixes in the codebase:

| Entity | Prefix | Constant |
|--------|--------|----------|
| Property | `prp` | `Property.PublicIdPrefix` |
| Unit | `unt` | `Unit.PublicIdPrefix` |
| Contact | `cnt` | `Contact.PublicIdPrefix` |
| Organization | `org` | `Organization.PublicIdPrefix` |
| Role | `rol` | `Role.PublicIdPrefix` |

Format: `{prefix}_{12_hex_chars}` (e.g., `cnt_a3f9c12d8e4b`).

Lookup entities (AmenityType, ComplianceType, Permission, PropertyPurpose, PropertyType) have no PublicId prefix.

### Entity Construction

```csharp
// From Contact.cs
public sealed class Contact : AggregateRoot
{
    public const string PublicIdPrefix = "cnt";

    private Contact() { }  // EF Core only

    public Contact(string displayName, string kind) : base(PublicIdPrefix)
    {
        DisplayName = displayName;
        Kind = kind;
    }

    // Domain event methods
    public void MarkAsCreated(Guid organizationId) => RaiseDomainEvent(new ContactCreatedEvent(Id, organizationId));
    public void MarkAsUpdated(Guid organizationId) => RaiseDomainEvent(new ContactUpdatedEvent(Id, organizationId));
    public void MarkAsArchived(Guid organizationId) => RaiseDomainEvent(new ContactArchivedEvent(Id, organizationId));
}
```

For entities that don't extend `AggregateRoot` (like `Role`), construct Id/PublicId manually:

```csharp
// From CreateRoleHandler.cs
var role = new Role
{
    Id = Guid.CreateVersion7(),
    PublicId = Domain.Common.PublicIdGenerator.Generate(Role.PublicIdPrefix),
    OrganizationId = currentUser.OrganizationId,
    Name = request.Name,
};
```

### ValueObjects

```csharp
// From Money.cs -- private constructor, static Create factory
public sealed class Money : ValueObject
{
    public long MinorUnits { get; }
    public string CurrencyCode { get; }

    private Money(long minorUnits, string currencyCode) { ... }

    public static Money Create(long minorUnits, string currencyCode) { ... }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinorUnits;
        yield return CurrencyCode;
    }
}
```

## API Endpoint Pattern

All endpoints use Minimal APIs with static method handlers. Pattern from actual files:

```csharp
// From RolesEndpoints.cs
internal static class RolesEndpoints
{
    public static void MapOrganizationRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/organizations/roles")
            .WithTags("Organization Roles")
            .RequireAuthorization();

        group.MapGet("/", GetRolesAsync)
            .WithName("GetOrganizationRoles")
            .WithSummary("Get all roles for the organization")
            .WithMetadata(new RequirePermissionAttribute("org:roles:read"))
            .Produces<IReadOnlyList<RoleResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateRoleAsync)
            .WithName("CreateRole")
            .WithSummary("Create a custom role")
            .WithMetadata(new RequirePermissionAttribute("org:roles:manage"))
            .Produces<string>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            // ...
    }

    private static async Task<IResult> CreateRoleAsync(CreateRoleRequest request, ISender sender, CancellationToken ct)
    {
        var command = new CreateRoleCommand(request.Name, request.Description, request.PermissionCodes);
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/v1/organizations/roles/{id}");
    }
}
```

Rules:
- Endpoint classes are `internal static`.
- URL pattern: `/v1/{resource}` (no `/api` prefix).
- Extension method: `Map{Feature}Endpoints(this IEndpointRouteBuilder app)`.
- All endpoints MUST be registered in `EndpointExtensions.MapApiEndpoints()`.
- Handler methods are `private static async Task<IResult>`.
- Use `result.ToHttpResult()` for 200/204 responses, `result.ToCreatedResult(...)` for 201.
- Permission: `.WithMetadata(new RequirePermissionAttribute("scope:action"))`.
- Paginated query params: use `[AsParameters]` record.

```csharp
// From MembersEndpoints.cs
private sealed record GetMembersQueryParams(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null,
    string? RoleId = null);
```

## Contracts Layer

Request/Response DTOs live in `Propentro.Contracts/{Feature}/`. Contracts CANNOT reference Application types (like `PagedResult<T>`).

### Request Records

Positional records for requests (deserialized from JSON body, not Dapper):

```csharp
// From CreateRoleRequest.cs
public sealed record CreateRoleRequest(string Name, string? Description, string[] PermissionCodes);

// From InviteMemberRequest.cs
public sealed record InviteMemberRequest(string Email, string RoleId);
```

### Response Records

Two styles depending on use:

**Positional record** -- when response is constructed in code (not a Dapper target):

```csharp
// From MeResponse.cs
public sealed record MeResponse(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    MeOrganizationResponse Organization,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
```

**Non-positional record with `init`** -- when used as a Dapper target (Dapper sets `init` via reflection):

```csharp
// From OrganizationSettingsResponse.cs
public sealed record OrganizationSettingsResponse
{
    public string OrganizationId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? ContactName { get; init; }
    // ...
}
```

DO NOT use positional records as Dapper targets -- constructor parameter matching breaks with nullable fields.

### Dapper Result Types Internal to Handlers

When query handlers need intermediate mapping (e.g., joining internal IDs not exposed in the response), use a `private sealed class` nested in the handler:

```csharp
// From GetOrganizationMembersHandler.cs
private sealed class MemberQueryResult
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public Guid UserInternalId { get; init; }  // Internal, not in response
}
```

## Validation

FluentValidation. Validators live alongside their command/query.

```csharp
// From InviteMemberValidator.cs
public sealed partial class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .Must(id => RoleIdRegex().IsMatch(id))
            .WithMessage("Invalid role ID format.");
    }

    [GeneratedRegex(@"^rol_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex RoleIdRegex();
}
```

### PublicId Validation

Always validate PublicId format before hitting DB. Use `[GeneratedRegex]` on `partial class`:

```csharp
[GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
private static partial Regex ContactIdRegex();
```

### Pagination Validation

All paginated queries MUST enforce: `Page >= 1`, `PageSize` between 1 and 100. Without this, `page=0` produces invalid SQL OFFSET and causes 500 errors.

```csharp
RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
```

## Multi-Tenancy

- **EF Core**: Global query filters in `AppDbContext.ApplyGlobalFilters()` auto-scope by `OrganizationId` and `IsActive`. No manual filter needed.
- **Dapper**: MUST manually add `WHERE organization_id = @OrganizationId` in every query.
- **Cache keys**: Auto-prefixed with `org:{OrganizationId}:` by `QueryCachingBehavior`.
- When adding a new org-scoped entity: add `HasQueryFilter` in `AppDbContext.ApplyGlobalFilters()`.

Current global filters:
- Org-scoped + soft-delete: Property, Unit, Contact, Address, Document, PropertyCompliance, UnitCompliance, Activity, Tag
- Org-scoped only: Notification, PropertyContactRole, UnitContactRole, NotificationPreference
- Soft-delete only: User, Organization, Role, SystemAdministrator
- No filter: OutboxMessage (cross-tenant access needed)

## Audit Columns

`AppDbContext.SaveChangesAsync` automatically sets:
- On `EntityState.Added`: `CreatedAt`, `CreatedById`, `UpdatedAt`, `UpdatedById`, `OrganizationId`
- On `EntityState.Modified`: `UpdatedAt`, `UpdatedById`

DO NOT set these fields in handlers. Only set fields NOT in `IAuditableEntity` (e.g., `ArchivedAt`, `ArchivedById`).

## Domain Events & Outbox

Events are exclusively async via outbox. No synchronous MediatR dispatch.

### Flow

```
entity.MarkAs{Action}(orgId)
  -> RaiseDomainEvent(new XEvent(...))
  -> SaveChangesAsync (UnitOfWork)
  -> AppDbContext.PersistDomainEventsToOutbox() serializes to outbox_messages table
  -> ProcessOutboxMessagesJob (Quartz) polls and dispatches
```

### Domain Event Definition

```csharp
// From ContactCreatedEvent.cs
public record ContactCreatedEvent(Guid ContactId, Guid OrganizationId)
    : DomainEvent(ContactId, OrganizationId)
{
    public override string EventType => "contact.created";
}
```

Event location: `Domain/Entities/{Feature}/Events/` -- NOT a top-level `Domain/Events/`.

EventType format: `{aggregate}.{action}` (lowercase dotted).

### Current Event Types

`contact.created`, `contact.updated`, `contact.archived`, `contact.unarchived`, `contact.deleted`,
`property.created`, `property.updated`,
`membership.created`, `membership.removed`

### Outbox Event Handlers

Implement `IOutboxEventHandler`. Co-locate with the related command, not in a separate folder.

```csharp
// From MembershipCreatedEventHandler.cs
public sealed class MembershipCreatedEventHandler(
    IEmailService emailService,
    IDbConnectionFactory dbConnectionFactory) : IOutboxEventHandler
{
    public string EventType => "membership.created";

    public async Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<MembershipCreatedEvent>(payload, OutboxJsonOptions.Instance);
        // ... send email ...
    }
}
```

- ALWAYS use `OutboxJsonOptions.Instance` (camelCase) -- matches AppDbContext serialization.
- Handlers are auto-registered by scanning `IOutboxEventHandler` implementations in the Application assembly.
- Unhandled event types are logged as warnings and marked completed.

### Manual Outbox Events

For entities that don't extend `AggregateRoot` (e.g., identity entities managed by webhook handlers), use `dbContext.AddOutboxEvent(...)`:

```csharp
dbContext.AddOutboxEvent(new MembershipCreatedEvent(userId, organizationId));
```

### Outbox Job

`ProcessOutboxMessagesJob` (`[DisallowConcurrentExecution]`):
- Selects `pending` messages with `FOR UPDATE SKIP LOCKED`
- Each message processed in its own mini-transaction
- Success -> `completed`; Failure -> exponential backoff (`2^retryCount` sec); Max retries -> `failed`

## Caching

- Redis when `ConnectionStrings:Redis` is set; MemoryCache fallback (auto-detected in `DependencyInjection.cs`).
- Both implementations use per-key `SemaphoreSlim` locking in `GetOrCreateAsync` (stampede protection).
- Always use `GetOrCreateAsync` for manual caching, never manual get-then-set.
- Cache invalidation is a direct call in handlers. DO NOT use domain events for cache invalidation.

### Permission Cache

- Key: `permissions:{organizationId}:{userId}`, 5 min TTL.
- Invalidation: `IPermissionService.InvalidatePermissionsCacheAsync(userId, orgId)` for single user, `InvalidateOrganizationPermissionsCacheAsync(orgId)` for all users in org.

## Side-Effect Decision Guide

| Concern | Mechanism | Transaction |
|---------|-----------|-------------|
| Activity log | Direct write in handler | Same transaction (atomic) |
| Cache invalidation | Direct call in handler | Post-save, best-effort |
| Email / webhook / push | Domain event -> Outbox | Async, guaranteed delivery |

## HTTP Resilience

Use custom `AddStandardResilience()` from `Infrastructure/Resilience/ResiliencePolicies.cs`. NEVER use Microsoft's built-in `AddStandardResilienceHandler()`.

| Strategy | Config |
|----------|--------|
| Total timeout (outer) | 30s for entire retry cycle |
| Retry | 3 attempts, exponential backoff 500ms + jitter |
| Circuit breaker | Opens at 50% failure over 30s (min 10 req), stays open 30s |
| Per-attempt timeout (inner) | 10s |

Graceful degradation: if API key/secret missing, log warning and return `Result.Success()`.

```csharp
// From ResendEmailService.cs
if (string.IsNullOrWhiteSpace(_options.ApiKey))
{
    Log.Warning("Email API key is not configured -- skipping email to {To}", message.To);
    return Result.Success();
}
```

## Email

Via `IEmailService` -> `ResendEmailService`. Templates in `Application/Common/Email/EmailTemplates.cs`:
- `WelcomeEmail(firstName)`
- `MembershipAddedEmail(firstName, orgName)`
- `MembershipRemovedEmail(firstName, orgName)`

Each returns `EmailContent(Subject, HtmlBody, TextBody)` with HTML layout wrapper.

## Webhooks (Clerk)

- `POST /v1/webhooks/clerk` -- anonymous, no auth.
- Svix HMAC-SHA256 verification + replay prevention via `WebhookIdempotencyStore` (in-memory, `svix-id` key).
- On failure: log, remove idempotency entry, return 500 (Clerk retries).
- Handlers use `IgnoreQueryFilters()` (outside tenant context), are idempotent.

| Clerk Event | Command |
|-------------|---------|
| `user.created` | `SyncUserCommand` |
| `organization.created/updated` | `SyncOrganizationCommand` |
| `organization.deleted` | `DeactivateOrganizationCommand` |
| `organizationMembership.created` | `SyncMembershipCommand` |

## Database Conventions

- Tables: `snake_case` plural (e.g., `contacts`, `organization_users`).
- Columns: `snake_case` (e.g., `public_id`, `organization_id`, `created_at`).
- All entities: `id` (Guid v7, internal) + `public_id` (prefixed hex, user-facing).
- Org-scoped: `organization_id`. Soft delete: `is_active`. Audit: `created_at`, `created_by_id`, `updated_at`, `updated_by_id`.
- Timestamps: `DateTimeOffset` (maps to PostgreSQL TIMESTAMPTZ). `DateOnly` for date-only fields. NEVER `DateTime`.
- Money: stored as `long` (minor units), e.g., `default_rent_minor`.

### EF Configuration Pattern

One configuration class per entity in `Infrastructure/Persistence/Configurations/`. Explicit column mapping for every property:

```csharp
// From ContactConfiguration.cs
public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PublicId).HasColumnName("public_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        // ... every property explicitly mapped ...
    }
}
```

## Logging

Use Serilog static `Log` class -- NEVER inject `ILogger<T>`. Use structured `{PropertyName}` placeholders, never string interpolation.

```csharp
// DO
Log.Information("Processing {EventType} for user {UserId}", eventType, userId);
Log.Warning("Failed to sync for {OrganizationId}: {Error}", org.PublicId, syncResult.Error.Message);

// DON'T
Log.Information($"Processing {eventType} for user {userId}");  // String interpolation
_logger.LogInformation("...");  // Injected ILogger
```

## Code Style

Enforced by `.editorconfig`:
- 4 spaces, LF line endings, UTF-8
- File-scoped namespaces (warning level)
- `_camelCase` private fields (warning level)
- `var` when type is apparent; explicit for built-in types
- Primary constructors preferred
- `Async` suffix on async methods (suggestion level)
- `I` prefix on interfaces (warning level)
- Braces: `when_multiline` (suggestion)
- New lines before `{`, `else`, `catch`, `finally`

Suppressed analyzers: CA1515, CA1062, CA1848, CA2007, CA1034, CA1711, CA1716, CA1724, CA1054/55/56, CA1822 (suggestion), IDE0058.

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Command | `{Verb}{Entity}Command` | `CreateRoleCommand`, `InviteMemberCommand` |
| Query | `Get{Entity}(ById/s)Query` | `GetOrganizationRolesQuery`, `GetMeQuery` |
| Handler | `{Command/Query}Handler` | `CreateRoleHandler`, `GetOrganizationRolesHandler` |
| Validator | `{Command/Query}Validator` | `CreateRoleValidator`, `GetOrganizationMembersValidator` |
| Response | `{Entity}Response` / `{Entity}ListItemResponse` | `RoleResponse`, `MemberListItemResponse` |
| Request | `{Verb}{Entity}Request` | `CreateRoleRequest`, `InviteMemberRequest` |
| Errors | `{Entity}Errors` | `RoleErrors`, `MemberErrors`, `MeErrors` |
| Domain event | `{Entity}{Action}Event` | `ContactCreatedEvent`, `MembershipRemovedEvent` |
| Outbox handler | `{Entity}{Action}EventHandler` | `MembershipCreatedEventHandler` |
| Endpoint class | `{Feature}Endpoints` | `RolesEndpoints`, `MembersEndpoints` |
| EF Config | `{Entity}Configuration` | `ContactConfiguration`, `RoleConfiguration` |
| Permissions | `{scope}:{action}` | `org:roles:read`, `org:members:manage` |

## Common Task Checklists

### Adding a New Entity

1. Create entity class in `Domain/Entities/{Feature}/` extending `AggregateRoot` (or `OrganizationEntity`/`Entity`)
   - Define `public const string PublicIdPrefix = "xxx";`
   - Add private parameterless constructor for EF Core
   - Add public constructor calling `base(PublicIdPrefix)`
2. If aggregate root: add `Events/` folder with domain event records and `MarkAs{Action}` methods
3. Add `DbSet<T>` in both `AppDbContext` AND `IAppDbContext`
4. Add `HasQueryFilter` in `AppDbContext.ApplyGlobalFilters()` (org-scoped and/or soft-delete)
5. Create EF configuration in `Infrastructure/Persistence/Configurations/{Entity}Configuration.cs`
6. Create Request/Response types in `Contracts/{Feature}/`
7. Create Commands/Queries in `Application/Features/{Feature}/`
8. Create Endpoints class and register in `EndpointExtensions.MapApiEndpoints()`
9. Run `dotnet ef migrations add Add{Entity} --project src/Propentro.Infrastructure --startup-project src/Propentro.Api`

### Adding a New Command

1. Create `{Verb}{Entity}Command.cs` -- `sealed record` implementing `ICommand<TResponse>` or `ICommand`
2. Create `{Verb}{Entity}Handler.cs` -- DO NOT call `SaveChangesAsync`
3. Create `{Verb}{Entity}Validator.cs` with FluentValidation rules
4. Create request type in `Contracts/{Feature}/{Verb}{Entity}Request.cs`
5. Add endpoint method in `Api/Endpoints/{Feature}Endpoints.cs`
6. Add cache invalidation if related cached queries exist
7. Add domain event + `MarkAs{Action}` method if async side-effects are needed

### Adding a New Query

1. Create `Get{Entity}(ById/s)Query.cs` implementing `IQuery<TResponse>` (add `ICachedQuery<TResponse>` if cacheable)
2. Create handler using `IDbConnectionFactory` (Dapper, `await using` + `OpenAsync`)
3. Create response type in `Contracts/{Feature}/` (non-positional `sealed record` with `init` if Dapper target)
4. Add validator if paginated (enforce Page >= 1, PageSize 1-100)
5. Add endpoint method
6. If using `IReadOnlyList<T>` (non-paginated): use `.Produces<IReadOnlyList<T>>()`
7. If using `PagedResult<T>`: use `.Produces<PagedResult<T>>()`

### Adding a New Outbox Event Handler

1. Create domain event record in `Domain/Entities/{Feature}/Events/{Entity}{Action}Event.cs`
2. Add `MarkAs{Action}` method on the entity calling `RaiseDomainEvent`
3. Create handler class implementing `IOutboxEventHandler` -- co-locate with related command in `Application/Features/{Feature}/Commands/`
4. Match `EventType` string exactly to the domain event's `override string EventType`
5. Use `OutboxJsonOptions.Instance` for deserialization

## Important Files

| File | Purpose |
|------|---------|
| `Directory.Build.props` | TreatWarningsAsErrors, AnalysisLevel, net10.0 |
| `Directory.Packages.props` | Centralized NuGet versions |
| `global.json` | SDK pinning (10.0.102) |
| `.editorconfig` | Code style + analyzer suppressions |
| `Application/DependencyInjection.cs` | MediatR pipeline order + validator registration |
| `Infrastructure/DependencyInjection.cs` | DB, auth, caching, outbox, email, webhooks |
| `Infrastructure/Persistence/AppDbContext.cs` | Global filters + audit fields + outbox persistence |
| `Application/Common/Interfaces/Messaging/OutboxJsonOptions.cs` | camelCase JSON for outbox |
| `Api/Endpoints/EndpointExtensions.cs` | Central endpoint registration |
| `Api/Extensions/ResultExtensions.cs` | Result -> IResult mapping |
| `Api/Middleware/GlobalExceptionHandler.cs` | Exception -> ProblemDetails |

## DO / DON'T

**DO:**
- Use `Guid.CreateVersion7()` for new entity IDs
- Use `PublicIdGenerator.Generate(prefix)` for PublicIds
- Use `DateTimeOffset` for all timestamps
- Use Dapper for read queries, EF Core for writes
- Use `await using var connection = db.CreateConnection(); await connection.OpenAsync(ct);` for Dapper
- Use raw SQL strings with `"""` triple-quoted strings
- Use primary constructors for DI injection
- Return `Result<T>` / `Result` from all handlers
- Add `WHERE organization_id = @OrganizationId` in every Dapper query
- Validate PublicId format with `[GeneratedRegex]` before DB lookups
- Use structured logging: `Log.Information("Message {Param}", value)`
- Co-locate outbox event handlers with their related commands

**DON'T:**
- Call `SaveChangesAsync` in command handlers (UnitOfWork handles it)
- Set audit fields (CreatedAt, UpdatedAt, etc.) in handlers (AppDbContext handles it)
- Use `DateTime` -- always `DateTimeOffset`
- Inject `ILogger<T>` -- use Serilog static `Log` class
- Use string interpolation in log messages
- Use positional records as Dapper mapping targets
- Use Microsoft's `AddStandardResilienceHandler()` -- use custom `AddStandardResilience()`
- Use domain events for cache invalidation (use direct calls)
- Reference `PagedResult<T>` from Contracts layer
- Add endpoints without registering in `EndpointExtensions.MapApiEndpoints()`
- Create top-level `Domain/Events/` folder -- events go in `Domain/Entities/{Feature}/Events/`
- Use `/api/v1/` prefix -- use `/v1/` directly

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Org filter not applied (Dapper) | Add `WHERE organization_id = @OrganizationId` manually |
| Org filter not applied (EF Core) | Add `HasQueryFilter` in `AppDbContext.ApplyGlobalFilters()` |
| Domain events not persisted | Verify entity extends `AggregateRoot` or use `dbContext.AddOutboxEvent()` |
| Dapper mapping fails | Verify `MatchNamesWithUnderscores = true` in Program.cs, use non-positional records |
| Validation not running | Ensure validator is in same assembly, class is not abstract |
| Build fails with warnings | `TreatWarningsAsErrors` is on -- fix all warnings |
| Cache not invalidating | Use full key with `org:{OrganizationId}:` prefix |
| Outbox handler not firing | Check `EventType` string matches exactly |
| Page=0 causes 500 | Add `RuleFor(x => x.Page).GreaterThanOrEqualTo(1)` |

`LoggingBehavior` logs all requests with timing. User context enriched via `UserContextMiddleware`. Trace ID in error responses. Scalar docs at `/scalar/v1` in Development.
