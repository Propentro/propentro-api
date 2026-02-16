using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Activities.Commands.DeleteActivity;

public sealed partial class DeleteActivityValidator : AbstractValidator<DeleteActivityCommand>
{
    public DeleteActivityValidator()
    {
        RuleFor(x => x.ActivityId)
            .NotEmpty()
            .Must(id => ActivityIdRegex().IsMatch(id))
            .WithMessage("Invalid activity ID format. Expected format: act_xxxxxxxxxxxx.");
    }

    [GeneratedRegex(@"^act_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ActivityIdRegex();
}
