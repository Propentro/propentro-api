using FluentValidation;

namespace Propentro.Application.Features.Admin.Organizations.Commands.ReactivateOrganization;

public sealed class ReactivateOrganizationValidator : AbstractValidator<ReactivateOrganizationCommand>
{
    public ReactivateOrganizationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
