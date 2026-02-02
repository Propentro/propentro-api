using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Address : OrganizationEntity
{
    public string Line1 { get; set; } = default!;
    public string? Line2 { get; set; }
    public string City { get; set; } = default!;
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = default!;
}
