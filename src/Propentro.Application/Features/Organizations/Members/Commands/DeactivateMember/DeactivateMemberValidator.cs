using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Organizations.Members.Commands.DeactivateMember;

public sealed partial class DeactivateMemberValidator : AbstractValidator<DeactivateMemberCommand>
{
    public DeactivateMemberValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(id => UserIdRegex().IsMatch(id))
            .WithMessage("Invalid user ID format.");
    }

    [GeneratedRegex(@"^usr_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex UserIdRegex();
}
