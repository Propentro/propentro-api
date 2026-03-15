namespace Propentro.Application.Features.Admin.Organizations.Commands.DeactivateOrganization;

public sealed record DeactivateOrganizationCommand(string PublicId) : ICommand;
