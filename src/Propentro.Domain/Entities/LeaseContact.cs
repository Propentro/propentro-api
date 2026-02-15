namespace Propentro.Domain.Entities;

public sealed class LeaseContact
{
    public Guid LeaseId { get; set; }
    public Guid ContactId { get; set; }
    public string Role { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public bool IsSignatory { get; set; } = true;
    public bool IsOccupant { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Lease Lease { get; set; } = default!;
    public Contact Contact { get; set; } = default!;
}
