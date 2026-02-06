using System.Linq.Expressions;
using System.Reflection;
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
    public DbSet<LeaseParty> LeaseParties => Set<LeaseParty>();

    // Parties
    public DbSet<Party> Parties => Set<Party>();

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
        var orgSoftDeleteMethod = GetType().GetMethod(
            nameof(SetOrgAndSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;
        var orgOnlyMethod = GetType().GetMethod(
            nameof(SetOrgOnlyFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;
        var softDeleteMethod = GetType().GetMethod(
            nameof(SetSoftDeleteOnlyFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isOrgScoped = typeof(IOrganizationScoped).IsAssignableFrom(clrType);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);

            if (isOrgScoped && isSoftDeletable)
                orgSoftDeleteMethod.MakeGenericMethod(clrType).Invoke(this, [builder]);
            else if (isOrgScoped)
                orgOnlyMethod.MakeGenericMethod(clrType).Invoke(this, [builder]);
            else if (isSoftDeletable)
                softDeleteMethod.MakeGenericMethod(clrType).Invoke(this, [builder]);
        }
    }

    // Lambda closures reference this.CurrentOrganizationId — EF Core rebinds per query
    private void SetOrgAndSoftDeleteFilter<T>(ModelBuilder b) where T : class, IOrganizationScoped, ISoftDeletable
        => b.Entity<T>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId && e.IsActive);

    private void SetOrgOnlyFilter<T>(ModelBuilder b) where T : class, IOrganizationScoped
        => b.Entity<T>().HasQueryFilter(e => e.OrganizationId == CurrentOrganizationId);

    private void SetSoftDeleteOnlyFilter<T>(ModelBuilder b) where T : class, ISoftDeletable
        => b.Entity<T>().HasQueryFilter(e => e.IsActive);

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
