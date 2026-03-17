using FluentValidation;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationValidator : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
