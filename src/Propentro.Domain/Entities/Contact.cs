using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Contact : AggregateRoot
{
    public string Kind { get; set; } = default!;
    public string DisplayName { get; set; } = default!;

    // Person
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Company
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }

    // Primary contact
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Secondary contact
    public string? SecondaryEmail { get; set; }
    public string? SecondaryPhone { get; set; }

    // Tenant-specific
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // Supplier-specific
    public string? Trade { get; set; }
    public string? TradeOther { get; set; }

    // Address
    public Guid? AddressId { get; set; }

    // Tax
    public string? TaxNumber { get; set; }
    public string? TaxCountry { get; set; }

    public string? Note { get; set; }

    // Archive (separate from is_active soft delete)
    public bool IsArchived { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? ArchivedById { get; set; }

    // Navigation
    public Address? Address { get; set; }
    public ICollection<ContactRole> ContactRoles { get; set; } = [];
    public ICollection<ContactDocument> ContactDocuments { get; set; } = [];

    private Contact() { }

    public Contact(string displayName, string kind) : base("cnt")
    {
        DisplayName = displayName;
        Kind = kind;
    }
}
