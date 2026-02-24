namespace Propentro.Domain.Entities.Lookups;

public sealed class Country
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? DialCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? RegionLabel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public Currency? Currency { get; set; }
}
