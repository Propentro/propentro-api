using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Both Id (Guid v7) and PublicId (alphanumeric) are generated client-side.
/// </summary>
public abstract class Entity : IHasPublicId
{
    public Guid Id { get; set; }
    public string PublicId { get; set; } = default!;

    protected Entity()
    {
        Id = Guid.CreateVersion7();
        PublicId = PublicIdGenerator.Generate();
    }
}
