using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Activity : OrganizationEntity
{
    public string EntityType { get; set; } = default!;
    public Guid EntityId { get; set; }

    public string ActivityType { get; set; } = default!;

    public string? Title { get; set; }
    public string? Description { get; set; }

    public string Metadata { get; set; } = "{}";

    public bool IsPinned { get; set; }

    private Activity() { }

    public Activity(string entityType, Guid entityId, string activityType) : base("act")
    {
        EntityType = entityType;
        EntityId = entityId;
        ActivityType = activityType;
    }
}
