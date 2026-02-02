namespace Propentro.Domain.Entities;

public sealed class NotificationTemplate
{
    public string Code { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string TitleTemplate { get; set; } = default!;
    public string BodyTemplate { get; set; } = default!;
    public string? EmailSubject { get; set; }
    public string? EmailBodyHtml { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
