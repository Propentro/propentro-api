namespace Propentro.Domain.Entities;

public sealed class PropertyDocument
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Property Property { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
