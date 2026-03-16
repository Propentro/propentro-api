using FluentValidation;

namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateUser;

public sealed class DeactivateUserValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
