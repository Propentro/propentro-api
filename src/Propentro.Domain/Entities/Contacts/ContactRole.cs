namespace Propentro.Domain.Entities.Contacts;

public sealed class ContactRole
{
    public Guid ContactId { get; set; }
    public string Role { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Contact Contact { get; set; } = default!;
}
