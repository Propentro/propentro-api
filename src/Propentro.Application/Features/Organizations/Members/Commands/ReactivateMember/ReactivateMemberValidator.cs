using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Organizations.Members.Commands.ReactivateMember;

public sealed partial class ReactivateMemberValidator : AbstractValidator<ReactivateMemberCommand>
{
    public ReactivateMemberValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(id => UserIdRegex().IsMatch(id))
            .WithMessage("Invalid user ID format.");
    }

    [GeneratedRegex(@"^usr_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex UserIdRegex();
}
