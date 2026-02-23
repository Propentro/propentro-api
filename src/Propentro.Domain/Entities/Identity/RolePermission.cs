using Propentro.Domain.Entities.Lookups;

namespace Propentro.Domain.Entities.Identity;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Role Role { get; set; } = default!;
    public Permission Permission { get; set; } = default!;
}
