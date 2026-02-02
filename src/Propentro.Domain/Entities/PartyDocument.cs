namespace Propentro.Domain.Entities;

public sealed class PartyDocument
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Party Party { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
