namespace Propentro.Domain.Entities;

public sealed class LeaseParty
{
    public Guid LeaseId { get; set; }
    public Guid PartyId { get; set; }
    public string Role { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public bool IsSignatory { get; set; } = true;
    public bool IsOccupant { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Lease Lease { get; set; } = default!;
    public Party Party { get; set; } = default!;
}
