using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

public abstract class AggregateRoot : OrganizationEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();

    protected AggregateRoot() { }

    protected AggregateRoot(string prefix) : base(prefix) { }
}
