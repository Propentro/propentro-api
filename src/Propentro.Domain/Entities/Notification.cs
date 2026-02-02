using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities;

public sealed class Notification : Entity, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Severity { get; set; } = "info";
    public string Data { get; set; } = "{}";
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? SenderId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = default!;
    public User? Sender { get; set; }
}
