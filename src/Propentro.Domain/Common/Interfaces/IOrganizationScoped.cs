namespace Propentro.Domain.Common.Interfaces;

public interface IOrganizationScoped
{
    Guid OrganizationId { get; set; }
}
