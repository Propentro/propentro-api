using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Use Entity(prefix) for new entities, parameterless for EF Core.
/// </summary>
public abstract class Entity : IHasPublicId
{
    public Guid Id { get; set; }
    public string PublicId { get; set; } = default!;

    protected Entity() { }

    protected Entity(string prefix)
    {
        Id = Guid.CreateVersion7();
        PublicId = PublicIdGenerator.Generate(prefix);
    }
}
