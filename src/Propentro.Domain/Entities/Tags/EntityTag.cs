namespace Propentro.Domain.Entities.Tags;

public sealed class EntityTag
{
    public Guid TagId { get; set; }
    public string EntityType { get; set; } = default!;
    public Guid EntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }

    // Navigation
    public Tag Tag { get; set; } = default!;
}
