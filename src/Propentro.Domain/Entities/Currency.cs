namespace Propentro.Domain.Entities;

public sealed class Currency
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Symbol { get; set; }
    public short MinorUnits { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
