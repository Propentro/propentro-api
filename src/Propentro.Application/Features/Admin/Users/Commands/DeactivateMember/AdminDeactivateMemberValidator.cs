using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

public sealed partial class AdminDeactivateMemberValidator : AbstractValidator<AdminDeactivateMemberCommand>
{
    public AdminDeactivateMemberValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty()
            .Must(id => OrgIdRegex().IsMatch(id))
            .WithMessage("Invalid organization ID format.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(id => UserIdRegex().IsMatch(id))
            .WithMessage("Invalid user ID format.");
    }

    [GeneratedRegex(@"^org_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex OrgIdRegex();

    [GeneratedRegex(@"^usr_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex UserIdRegex();
}
