using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Activities.Commands.UpdateActivity;

public sealed partial class UpdateActivityValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityValidator()
    {
        RuleFor(x => x.ActivityId)
            .NotEmpty()
            .Must(id => ActivityIdRegex().IsMatch(id))
            .WithMessage("Invalid activity ID format.");

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => x.Description is not null);
    }

    [GeneratedRegex(@"^act_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ActivityIdRegex();
}
