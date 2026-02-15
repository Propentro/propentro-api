namespace Propentro.Domain.Entities;

public sealed class ContactRole
{
    public Guid ContactId { get; set; }
    public string Role { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Contact Contact { get; set; } = default!;
}
