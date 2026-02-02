using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

public abstract class Entity : IHasPublicId
{
    public Guid Id { get; set; }
    public string PublicId { get; set; } = default!;
}
