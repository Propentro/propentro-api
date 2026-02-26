using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public sealed partial class CreateActivityValidator : AbstractValidator<CreateActivityCommand>
{
    private static readonly string[] _userActivityTypes = ["note", "phone_call"];

    public CreateActivityValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(t => t == "contact")
            .WithMessage("Entity type must be 'contact'.");

        RuleFor(x => x.EntityId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .When(x => x.EntityType == "contact")
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.ActivityType)
            .NotEmpty()
            .Must(t => _userActivityTypes.Contains(t))
            .WithMessage("Activity type must be 'note' or 'phone_call'.");

        RuleFor(x => x.Description)
            .MaximumLength(5000);
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
