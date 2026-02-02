using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces;
using Propentro.Domain.Common.Interfaces;
using Propentro.Domain.Entities;

namespace Propentro.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser,
    IDateTimeProvider dateTime) : DbContext(options), IAppDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
    public DbSet<SystemAdministrator> SystemAdministrators => Set<SystemAdministrator>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<LeaseUnit> LeaseUnits => Set<LeaseUnit>();
    public DbSet<LeaseParty> LeaseParties => Set<LeaseParty>();
    public DbSet<PropertyCompliance> PropertyCompliances => Set<PropertyCompliance>();
    public DbSet<UnitCompliance> UnitCompliances => Set<UnitCompliance>();
    public DbSet<LeaseCompliance> LeaseCompliances => Set<LeaseCompliance>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();
    public DbSet<UnitAmenity> UnitAmenities => Set<UnitAmenity>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OrganizationUserRole> OrganizationUserRoles => Set<OrganizationUserRole>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter for multi-tenancy
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IOrganizationScoped).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildOrganizationFilter(entityType.ClrType));
            }

            // Soft delete filter
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private System.Linq.Expressions.LambdaExpression BuildOrganizationFilter(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(IOrganizationScoped.OrganizationId));
        var currentOrgId = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Constant(this),
            "CurrentOrganizationId");
        var equals = System.Linq.Expressions.Expression.Equal(property, currentOrgId);
        return System.Linq.Expressions.Expression.Lambda(equals, parameter);
    }

    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsActive));
        var trueConstant = System.Linq.Expressions.Expression.Constant(true);
        var equals = System.Linq.Expressions.Expression.Equal(property, trueConstant);
        return System.Linq.Expressions.Expression.Lambda(equals, parameter);
    }

    public Guid CurrentOrganizationId => currentUser.OrganizationId;

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = dateTime.UtcNow;
        var userId = currentUser.UserId is not null ? Guid.Parse(currentUser.UserId) : (Guid?)null;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedById = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedById = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IOrganizationScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.OrganizationId == Guid.Empty)
            {
                entry.Entity.OrganizationId = currentUser.OrganizationId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IHasPublicId>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.PublicId))
            {
                entry.Entity.PublicId = GeneratePublicId(entry.Entity.GetType());
            }
        }

        return await base.SaveChangesAsync(ct);
    }

    private static string GeneratePublicId(Type entityType)
    {
        var prefix = entityType.Name switch
        {
            nameof(Property) => "prp",
            nameof(Unit) => "unt",
            nameof(Lease) => "lse",
            nameof(Party) => "pty",
            nameof(Address) => "adr",
            nameof(Document) => "doc",
            nameof(Organization) => "org",
            nameof(User) => "usr",
            nameof(Role) => "rol",
            nameof(Permission) => "prm",
            nameof(Notification) => "ntf",
            _ => "gen"
        };

        var uniquePart = Guid.NewGuid().ToString("N")[..12];
        return $"{prefix}_{uniquePart}";
    }
}
