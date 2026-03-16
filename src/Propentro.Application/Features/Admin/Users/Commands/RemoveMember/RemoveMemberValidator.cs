using FluentValidation;

namespace Propentro.Application.Features.Admin.Users.Commands.RemoveMember;

public sealed class RemoveMemberValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
