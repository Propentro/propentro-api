using FluentValidation;

namespace Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase alphanumeric with hyphens");
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => x.ContactEmail is not null);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubscriptionPlan).NotEmpty()
            .Must(p => p is "starter" or "pro" or "enterprise");
    }
}
