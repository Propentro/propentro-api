using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public sealed partial class CreateActivityValidator : AbstractValidator<CreateActivityCommand>
{
    private static readonly string[] _validTypes = ["note", "phone_call"];

    public CreateActivityValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format. Expected format: cnt_xxxxxxxxxxxx (cnt_ followed by 12 hex characters).");

        RuleFor(x => x.ActivityType)
            .NotEmpty()
            .Must(t => _validTypes.Contains(t))
            .WithMessage("Activity type must be 'note' or 'phone_call'.");

        RuleFor(x => x.Title).MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(5000);
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
