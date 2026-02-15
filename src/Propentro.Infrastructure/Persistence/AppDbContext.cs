using MediatR;
using Microsoft.EntityFrameworkCore;
using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;
using Propentro.Domain.Entities;
using Unit = Propentro.Domain.Entities.Unit;

namespace Propentro.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context with automatic organization scoping,
/// soft delete filtering, audit field management, and domain event publishing.
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser,
    IDateTimeProvider dateTime,
    IPublisher publisher) : DbContext(options), IAppDbContext
{
    // Organization & User Management
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
    public DbSet<SystemAdministrator> SystemAdministrators => Set<SystemAdministrator>();

    // Property Management
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Address> Addresses => Set<Address>();

    // Lease Management
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<LeaseUnit> LeaseUnits => Set<LeaseUnit>();
    public DbSet<LeaseContact> LeaseContacts => Set<LeaseContact>();

    // Contacts
    public DbSet<Contact> Contacts => Set<Contact>();

    // Activities
    public DbSet<Activity> Activities => Set<Activity>();

    // Documents
    public DbSet<Document> Documents => Set<Document>();

    // Compliance
    public DbSet<PropertyCompliance> PropertyCompliances => Set<PropertyCompliance>();
    public DbSet<UnitCompliance> UnitCompliances => Set<UnitCompliance>();
    public DbSet<LeaseCompliance> LeaseCompliances => Set<LeaseCompliance>();

    // Amenities
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();
    public DbSet<UnitAmenity> UnitAmenities => Set<UnitAmenity>();

    // Authorization
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OrganizationUserRole> OrganizationUserRoles => Set<OrganizationUserRole>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Current organization ID for global query filter.
    /// EF Core rebinds this per query since it's a DbContext member access.
    /// </summary>
    internal Guid CurrentOrganizationId => currentUser.OrganizationId;

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
        builder.Entity<Lease>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<Contact>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<Address>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<Document>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<PropertyCompliance>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<UnitCompliance>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);
        builder.Entity<LeaseCompliance>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);

        // Organization-scoped only
        builder.Entity<Notification>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
        builder.Entity<PropertyContactRole>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);
        builder.Entity<UnitContactRole>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);

        // Activity: Organization-scoped + soft-delete
        builder.Entity<Activity>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);

        // Soft-delete only
        builder.Entity<User>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Organization>().HasQueryFilter(e => e.IsActive);
        builder.Entity<Role>().HasQueryFilter(e => e.IsActive);
        builder.Entity<SystemAdministrator>().HasQueryFilter(e => e.IsActive);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = dateTime.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry is { Entity: IOrganizationScoped scoped, State: EntityState.Added })
                scoped.OrganizationId = currentUser.OrganizationId;

            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedById = userId;
                }

                if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedById = userId;
                }
            }
        }

        var domainEvents = ChangeTracker.Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var @event in domainEvents)
            await publisher.Publish(@event, cancellationToken);

        foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
            entry.Entity.ClearDomainEvents();

        return result;
    }
}
