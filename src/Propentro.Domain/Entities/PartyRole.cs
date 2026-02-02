namespace Propentro.Domain.Entities;

public sealed class PartyRole
{
    public Guid PartyId { get; set; }
    public string Role { get; set; } = default!;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Party Party { get; set; } = default!;
}
