using Propentro.Domain.Entities.Documents;

namespace Propentro.Domain.Entities.Contacts;

public sealed class ContactDocument
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public Guid DocumentId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Contact Contact { get; set; } = default!;
    public Document Document { get; set; } = default!;
}
