using Microsoft.EntityFrameworkCore;
using Propentro.Domain.Entities;

namespace Propentro.Application.Common.Interfaces.Persistence;

/// <summary>
/// Abstraction for the Entity Framework Core database context.
/// Provides access to entity sets with automatic organization scoping and soft-delete filtering.
/// </summary>
public interface IAppDbContext
{
    // Organization & User Management
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<OrganizationUser> OrganizationUsers { get; }
    DbSet<SystemAdministrator> SystemAdministrators { get; }

    // Property Management
    DbSet<Property> Properties { get; }
    DbSet<Unit> Units { get; }
    DbSet<Address> Addresses { get; }

    // Lease Management
    DbSet<Lease> Leases { get; }
    DbSet<LeaseUnit> LeaseUnits { get; }
    DbSet<LeaseParty> LeaseParties { get; }

    // Parties
    DbSet<Party> Parties { get; }

    // Documents
    DbSet<Document> Documents { get; }

    // Compliance
    DbSet<PropertyCompliance> PropertyCompliances { get; }
    DbSet<UnitCompliance> UnitCompliances { get; }
    DbSet<LeaseCompliance> LeaseCompliances { get; }

    // Amenities
    DbSet<PropertyAmenity> PropertyAmenities { get; }
    DbSet<UnitAmenity> UnitAmenities { get; }

    // Authorization
    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<OrganizationUserRole> OrganizationUserRoles { get; }

    // Notifications
    DbSet<Notification> Notifications { get; }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// Automatically sets audit fields (CreatedAt, UpdatedAt, CreatedById, UpdatedById)
    /// and publishes domain events after successful save.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
