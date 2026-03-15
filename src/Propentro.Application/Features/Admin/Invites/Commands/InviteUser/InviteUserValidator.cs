using FluentValidation;

namespace Propentro.Application.Features.Admin.Invites.Commands.InviteUser;

public sealed class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.OrgPublicId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.RolePublicId).NotEmpty();
    }
}
