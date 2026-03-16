using FluentValidation;

namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateUser;

public sealed class ReactivateUserValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
