namespace Propentro.Domain.Entities;

public sealed class NotificationPreference
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Category { get; set; } = default!;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public Organization Organization { get; set; } = default!;
    public User User { get; set; } = default!;
}
