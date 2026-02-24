using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Activity = Propentro.Domain.Entities.Activities.Activity;

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

    // Contacts
    DbSet<Contact> Contacts { get; }
    DbSet<ContactRole> ContactRoles { get; }
    DbSet<ContactDocument> ContactDocuments { get; }
    DbSet<PropertyContactRole> PropertyContactRoles { get; }
    DbSet<UnitContactRole> UnitContactRoles { get; }

    // Activities
    DbSet<Activity> Activities { get; }

    // Documents
    DbSet<Document> Documents { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<PropertyDocument> PropertyDocuments { get; }
    DbSet<UnitDocument> UnitDocuments { get; }

    // Compliance
    DbSet<PropertyCompliance> PropertyCompliances { get; }
    DbSet<UnitCompliance> UnitCompliances { get; }
    DbSet<ComplianceType> ComplianceTypes { get; }
    DbSet<PropertyComplianceDocument> PropertyComplianceDocuments { get; }
    DbSet<UnitComplianceDocument> UnitComplianceDocuments { get; }

    // Amenities
    DbSet<PropertyAmenity> PropertyAmenities { get; }
    DbSet<UnitAmenity> UnitAmenities { get; }
    DbSet<AmenityType> AmenityTypes { get; }

    // Authorization
    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<OrganizationUserRole> OrganizationUserRoles { get; }

    // Notifications
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }

    // Lookup Tables
    DbSet<Currency> Currencies { get; }
    DbSet<Country> Countries { get; }
    DbSet<PropertyPurpose> PropertyPurposes { get; }
    DbSet<PropertyType> PropertyTypes { get; }

    // Outbox
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>
    /// Provides access to database-related information and operations.
    /// Used for transaction management in UnitOfWork pattern.
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// Automatically sets audit fields (CreatedAt, UpdatedAt, CreatedById, UpdatedById)
    /// and publishes domain events after successful save.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
