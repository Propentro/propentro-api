using FluentValidation;

namespace Propentro.Application.Features.Organizations.Invites.Commands.InviteToCurrentOrg;

public sealed class InviteToCurrentOrgValidator : AbstractValidator<InviteToCurrentOrgCommand>
{
    public InviteToCurrentOrgValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
