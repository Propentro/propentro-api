using FluentValidation;

namespace Propentro.Application.Features.Organizations.Commands.UpdateOrganizationSettings;

public sealed class UpdateOrganizationSettingsValidator : AbstractValidator<UpdateOrganizationSettingsCommand>
{
    public UpdateOrganizationSettingsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => x.ContactEmail is not null);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(50)
            .When(x => x.ContactPhone is not null);

        RuleFor(x => x.AddressLine1).MaximumLength(255).When(x => x.AddressLine1 is not null);
        RuleFor(x => x.AddressLine2).MaximumLength(255).When(x => x.AddressLine2 is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.Region).MaximumLength(100).When(x => x.Region is not null);
        RuleFor(x => x.PostalCode).MaximumLength(20).When(x => x.PostalCode is not null);

        RuleFor(x => x.CountryCode)
            .Length(2)
            .When(x => x.CountryCode is not null);

        RuleFor(x => x.LegalName).MaximumLength(255).When(x => x.LegalName is not null);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber is not null);
        RuleFor(x => x.TaxCountry).MaximumLength(100).When(x => x.TaxCountry is not null);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(100);
    }
}
