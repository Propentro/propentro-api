using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Party : AggregateRoot
{
    public string Kind { get; set; } = default!;
    public string DisplayName { get; set; } = default!;

    // Person fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Organization fields
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }

    // Contact
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Address
    public Guid? AddressId { get; set; }

    // Tax
    public string? TaxNumber { get; set; }
    public string? TaxCountry { get; set; }

    public string? Note { get; set; }

    // Navigation
    public Address? Address { get; set; }
    public ICollection<PartyRole> PartyRoles { get; set; } = [];
    public ICollection<PartyDocument> PartyDocuments { get; set; } = [];
}
