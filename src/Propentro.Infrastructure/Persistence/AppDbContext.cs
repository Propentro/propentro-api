using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;
using Serilog;
using Activity = Propentro.Domain.Entities.Activities.Activity;
using Unit = Propentro.Domain.Entities.Units.Unit;

namespace Propentro.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context with automatic organization scoping,
/// soft delete filtering, audit field management, and outbox-based domain event persistence.
/// </summary>
public sealed partial class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IUserContext userContext,
    IDateTimeProvider dateTime) : DbContext(options), IAppDbContext
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Organization & User Management
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
    public DbSet<SystemAdministrator> SystemAdministrators => Set<SystemAdministrator>();

    // Property Management
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Address> Addresses => Set<Address>();

    // Contacts
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactRole> ContactRoles => Set<ContactRole>();
    public DbSet<ContactDocument> ContactDocuments => Set<ContactDocument>();
    public DbSet<PropertyContactRole> PropertyContactRoles => Set<PropertyContactRole>();
    public DbSet<UnitContactRole> UnitContactRoles => Set<UnitContactRole>();

    // Activities
    public DbSet<Activity> Activities => Set<Activity>();

    // Documents
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<PropertyDocument> PropertyDocuments => Set<PropertyDocument>();
    public DbSet<UnitDocument> UnitDocuments => Set<UnitDocument>();

    // Compliance
    public DbSet<PropertyCompliance> PropertyCompliances => Set<PropertyCompliance>();
    public DbSet<UnitCompliance> UnitCompliances => Set<UnitCompliance>();
    public DbSet<ComplianceType> ComplianceTypes => Set<ComplianceType>();
    public DbSet<PropertyComplianceDocument> PropertyComplianceDocuments => Set<PropertyComplianceDocument>();
    public DbSet<UnitComplianceDocument> UnitComplianceDocuments => Set<UnitComplianceDocument>();

    // Amenities
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();
    public DbSet<UnitAmenity> UnitAmenities => Set<UnitAmenity>();
    public DbSet<AmenityType> AmenityTypes => Set<AmenityType>();

    // Authorization
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OrganizationUserRole> OrganizationUserRoles => Set<OrganizationUserRole>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // Lookup Tables
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<PropertyPurpose> PropertyPurposes => Set<PropertyPurpose>();
    public DbSet<PropertyType> PropertyTypes => Set<PropertyType>();

    // Outbox
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Current organization ID for global query filter.
    /// EF Core rebinds this per query since it's a DbContext member access.
    /// </summary>
    internal Guid CurrentOrganizationId => userContext.OrganizationId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyGlobalFilters(modelBuilder);
    }

    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        // Organization-scoped + soft-delete
        builder.Entity<Property>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<Unit>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<Contact>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<Address>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<Document>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<PropertyCompliance>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<UnitCompliance>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        // Organization-scoped only
        builder.Entity<Notification>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
        builder.Entity<PropertyContactRole>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
        builder.Entity<UnitContactRole>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
        builder.Entity<NotificationPreference>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);

        // Activity: Organization-scoped + soft-delete
        builder.Entity<Activity>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);

        // Soft-delete only
        builder.Entity<User>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Organization>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Role>().HasQueryFilter(e => e.IsActive);
        builder.Entity<SystemAdministrator>().HasQueryFilter(e => e.IsActive);

        // No global query filter for OutboxMessage — outbox worker needs cross-tenant access
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = dateTime.UtcNow;
        var userId = userContext.UserId;

        ApplyAuditFields(now, userId);
        PersistDomainEventsToOutbox(now, userId);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields(DateTimeOffset now, Guid userId)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry is { Entity: IOrganizationScoped scoped, State: EntityState.Added })
                scoped.OrganizationId = userContext.OrganizationId;

            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedById = userId;
                    auditable.UpdatedAt = now;
                    auditable.UpdatedById = userId;
                }

                if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedById = userId;
                }
            }
        }
    }

    private void PersistDomainEventsToOutbox(DateTimeOffset now, Guid userId)
    {
        // Collect domain events from aggregate roots
        var domainEvents = ChangeTracker.Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Serialize domain events to outbox messages (committed atomically with business data)
        foreach (var domainEvent in domainEvents)
        {
            var eventTypeName = DeriveEventType(domainEvent);
            var aggregateType = DeriveAggregateType(domainEvent);
            var aggregateId = ExtractAggregateId(domainEvent);
            var organizationId = ExtractOrganizationId(domainEvent);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = organizationId,
                EventType = eventTypeName,
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                Payload = JsonSerializer.Serialize<object>(domainEvent, _jsonOptions),
                Status = "pending",
                CreatedAt = now,
                CreatedById = userId != Guid.Empty ? userId : null
            };

            OutboxMessages.Add(outboxMessage);
        }

        // Clear domain events on entities before save
        foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
            entry.Entity.ClearDomainEvents();

        if (domainEvents.Count > 0)
            Log.Debug("Persisted {EventCount} domain events to outbox", domainEvents.Count);
    }

    /// <summary>
    /// Derives the event type string from a domain event.
    /// Convention: "aggregate.action" (e.g., ContactCreatedEvent → "contact.created")
    /// </summary>
    private static string DeriveEventType(IDomainEvent domainEvent)
    {
        var typeName = domainEvent.GetType().Name;

        // Remove "Event" suffix
        if (typeName.EndsWith("Event", StringComparison.Ordinal))
            typeName = typeName[..^5];

        // Split PascalCase into parts and convert to lowercase dot-separated
        var parts = PascalCaseSplitRegex().Split(typeName);
        var filtered = parts.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.ToLowerInvariant()).ToArray();

        // Convention: last part is the action (created, updated, activated), rest is the aggregate
        return filtered.Length >= 2
            ? $"{string.Join("_", filtered[..^1])}.{filtered[^1]}"
            : string.Join(".", filtered);
    }

    /// <summary>
    /// Derives the aggregate type from a domain event.
    /// Convention: first part of the event name (e.g., ContactCreatedEvent → "contact")
    /// </summary>
    private static string DeriveAggregateType(IDomainEvent domainEvent)
    {
        var typeName = domainEvent.GetType().Name;

        if (typeName.EndsWith("Event", StringComparison.Ordinal))
            typeName = typeName[..^5];

        var parts = PascalCaseSplitRegex().Split(typeName);
        var filtered = parts.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.ToLowerInvariant()).ToArray();

        // Aggregate type is everything except the last word (the action)
        return filtered.Length >= 2
            ? string.Join("_", filtered[..^1])
            : filtered.Length > 0 ? filtered[0] : "unknown";
    }

    /// <summary>
    /// Extracts the aggregate ID from a domain event via reflection.
    /// Looks for properties ending with "Id" that are Guid type (excluding OrganizationId).
    /// </summary>
    private static Guid ExtractAggregateId(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();
        var idProperty = eventType.GetProperties()
            .FirstOrDefault(p =>
                p.PropertyType == typeof(Guid) &&
                p.Name.EndsWith("Id", StringComparison.Ordinal) &&
                p.Name != "OrganizationId");

        if (idProperty is not null)
            return (Guid)idProperty.GetValue(domainEvent)!;

        Log.Warning("Could not extract aggregate ID from {EventType}", eventType.Name);
        return Guid.Empty;
    }

    /// <summary>
    /// Extracts the organization ID from a domain event via reflection.
    /// </summary>
    private static Guid ExtractOrganizationId(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();
        var orgProperty = eventType.GetProperty("OrganizationId");

        if (orgProperty is not null && orgProperty.PropertyType == typeof(Guid))
            return (Guid)orgProperty.GetValue(domainEvent)!;

        Log.Warning("Could not extract OrganizationId from {EventType}", eventType.Name);
        return Guid.Empty;
    }

    [GeneratedRegex("(?=[A-Z])")]
    private static partial Regex PascalCaseSplitRegex();
}
