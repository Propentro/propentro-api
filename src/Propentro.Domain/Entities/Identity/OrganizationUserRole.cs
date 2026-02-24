namespace Propentro.Domain.Entities.Identity;

public sealed class OrganizationUserRole
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }

    // Navigation
    public Organization Organization { get; set; } = default!;
    public User User { get; set; } = default!;
    public Role Role { get; set; } = default!;
    public User? AssignedByUser { get; set; }
}
