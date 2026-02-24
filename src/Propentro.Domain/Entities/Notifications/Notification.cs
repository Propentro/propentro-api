using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

using Propentro.Domain.Entities.Identity;

namespace Propentro.Domain.Entities.Notifications;

public sealed class Notification : Entity, IOrganizationScoped
{
    public const string PublicIdPrefix = "ntf";

    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Severity { get; set; } = "info";
    public string Data { get; set; } = "{}";
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public Guid? SenderId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = default!;
    public User? Sender { get; set; }
}
