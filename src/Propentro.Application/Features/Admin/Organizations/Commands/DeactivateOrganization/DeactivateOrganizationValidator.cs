using FluentValidation;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeactivateOrganization;

public sealed class DeactivateOrganizationValidator : AbstractValidator<DeactivateOrganizationCommand>
{
    public DeactivateOrganizationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
