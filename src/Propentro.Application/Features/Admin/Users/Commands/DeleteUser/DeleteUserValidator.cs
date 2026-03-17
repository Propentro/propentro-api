using FluentValidation;

namespace Propentro.Application.Features.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .Matches(@"^usr_[0-9a-f]{12}$")
            .WithMessage("Id must be a valid user public ID (e.g. usr_a1b2c3d4e5f6).");
    }
}
