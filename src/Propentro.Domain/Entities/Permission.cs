namespace Propentro.Domain.Entities;

public sealed class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string GroupName { get; set; } = default!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
