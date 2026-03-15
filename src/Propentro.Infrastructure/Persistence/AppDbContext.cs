using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;
using Propentro.Domain.Entities.Tags;
using Serilog;
using Activity = Propentro.Domain.Entities.Activities.Activity;
using Unit = Propentro.Domain.Entities.Units.Unit;

namespace Propentro.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context with automatic organization scoping,
/// soft delete filtering, audit field management, and outbox-based domain event persistence.
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IUserContext userContext,
    IDateTimeProvider dateTime) : DbContext(options), IAppDbContext
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly List<IDomainEvent> _pendingEvents = [];

    // Organization & User Management
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
    public DbSet<SystemAdministrator> SystemAdministrators => Set<SystemAdministrator>();
    public DbSet<Invite> Invites => Set<Invite>();

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
    public DbSet<PropertyPurpose> PropertyPurposes => Set<PropertyPurpose>();
    public DbSet<PropertyType> PropertyTypes => Set<PropertyType>();

    // Tags
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntityTag> EntityTags => Set<EntityTag>();

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

        // Tag: Organization-scoped + soft-delete
        builder.Entity<Tag>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);

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
        PersistDomainEventsToOutbox(userId);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields(DateTimeOffset now, Guid userId)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry is { Entity: IOrganizationScoped scoped, State: EntityState.Added }
                && userContext.OrganizationId != Guid.Empty
                && scoped.OrganizationId == Guid.Empty)
            {
                scoped.OrganizationId = userContext.OrganizationId;
            }

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

    /// <inheritdoc />
    public void AddOutboxEvent(IDomainEvent domainEvent) => _pendingEvents.Add(domainEvent);

    private void PersistDomainEventsToOutbox(Guid userId)
    {
        // Collect events from AggregateRoot entities (automatic)
        foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
        {
            _pendingEvents.AddRange(entry.Entity.DomainEvents);
            entry.Entity.ClearDomainEvents();
        }

        if (_pendingEvents.Count == 0)
            return;

        // Persist all events (both manual and AggregateRoot) to outbox
        foreach (var domainEvent in _pendingEvents)
            OutboxMessages.Add(CreateOutboxMessage(domainEvent, userId));

        Log.Debug("Persisted {EventCount} domain events to outbox", _pendingEvents.Count);
        _pendingEvents.Clear();
    }

    private static OutboxMessage CreateOutboxMessage(IDomainEvent domainEvent, Guid userId)
    {
        string eventType = domainEvent.EventType;

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = domainEvent.OrganizationId,
            EventType = eventType,
            AggregateType = eventType[..eventType.IndexOf('.')],
            AggregateId = domainEvent.AggregateId,
            Payload = JsonSerializer.Serialize<object>(domainEvent, _jsonOptions),
            Status = "pending",
            CreatedAt = domainEvent.OccurredAt,
            CreatedById = userId != Guid.Empty ? userId : null
        };
    }
}
