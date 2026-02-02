using Microsoft.EntityFrameworkCore;
using Propentro.Domain.Entities;

namespace Propentro.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<OrganizationUser> OrganizationUsers { get; }
    DbSet<SystemAdministrator> SystemAdministrators { get; }
    DbSet<Address> Addresses { get; }
    DbSet<Document> Documents { get; }
    DbSet<Party> Parties { get; }
    DbSet<Property> Properties { get; }
    DbSet<Unit> Units { get; }
    DbSet<Lease> Leases { get; }
    DbSet<LeaseUnit> LeaseUnits { get; }
    DbSet<LeaseParty> LeaseParties { get; }
    DbSet<PropertyCompliance> PropertyCompliances { get; }
    DbSet<UnitCompliance> UnitCompliances { get; }
    DbSet<LeaseCompliance> LeaseCompliances { get; }
    DbSet<PropertyAmenity> PropertyAmenities { get; }
    DbSet<UnitAmenity> UnitAmenities { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<OrganizationUserRole> OrganizationUserRoles { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
