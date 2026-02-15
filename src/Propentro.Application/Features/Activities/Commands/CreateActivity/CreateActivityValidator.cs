using FluentValidation;

namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public sealed class CreateActivityValidator : AbstractValidator<CreateActivityCommand>
{
    private static readonly string[] ValidTypes = ["note", "phone_call"];

    public CreateActivityValidator()
    {
        RuleFor(x => x.ContactId).NotEmpty();

        RuleFor(x => x.ActivityType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("Activity type must be 'note' or 'phone_call'.");

        RuleFor(x => x.Title).MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(5000);
    }
}
