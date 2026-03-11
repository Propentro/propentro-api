using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Tags;

public sealed class Tag : OrganizationEntity
{
    public const string PublicIdPrefix = "tag";

    public string Name { get; set; } = default!;
    public string? Color { get; set; }

    private Tag() { }

    public Tag(string name, string? color) : base(PublicIdPrefix)
    {
        Name = name;
        Color = color;
    }
}
