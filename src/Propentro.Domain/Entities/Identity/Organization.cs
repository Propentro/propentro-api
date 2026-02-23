using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities.Identity;

public sealed class Organization : Entity, IAuditableEntity, ISoftDeletable
{
    public const string PublicIdPrefix = "org";

    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;

    // Auth Provider (Clerk, Entra, etc.)
    public string? ExternalId { get; set; }
    public string? AuthProvider { get; set; }

    // Primary contact
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // Company address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }

    // Legal/tax
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxCountry { get; set; }

    // Business settings
    public string CurrencyCode { get; set; } = "EUR";
    public string Timezone { get; set; } = "UTC";
    public string SubscriptionPlan { get; set; } = "starter";

    // JSONB settings (stored as string, parsed in app layer)
    public string UiSettings { get; set; } = "{}";
    public string NotificationSettings { get; set; } = "{}";

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // ISoftDeletable
    public bool IsActive { get; set; } = true;
}
