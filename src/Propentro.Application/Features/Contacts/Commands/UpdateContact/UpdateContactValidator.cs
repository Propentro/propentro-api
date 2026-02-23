using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.UpdateContact;

public sealed partial class UpdateContactValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.LegalName).MaximumLength(200).When(x => x.LegalName is not null);
        RuleFor(x => x.TradingName).MaximumLength(200).When(x => x.TradingName is not null);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
        RuleFor(x => x.SecondaryEmail).EmailAddress().When(x => x.SecondaryEmail is not null);
        RuleFor(x => x.SecondaryPhone).MaximumLength(50).When(x => x.SecondaryPhone is not null);
        RuleFor(x => x.EmergencyContactName).MaximumLength(200).When(x => x.EmergencyContactName is not null);
        RuleFor(x => x.EmergencyContactPhone).MaximumLength(50).When(x => x.EmergencyContactPhone is not null);
        RuleFor(x => x.Trade).MaximumLength(100).When(x => x.Trade is not null);
        RuleFor(x => x.TradeOther).MaximumLength(100).When(x => x.TradeOther is not null);
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => x.TaxNumber is not null);
        RuleFor(x => x.TaxCountry).MaximumLength(2).When(x => x.TaxCountry is not null);
        RuleFor(x => x.Note).MaximumLength(2000).When(x => x.Note is not null);

        When(x => x.Address is not null, () =>
        {
            RuleFor(x => x.Address!.Line1).MaximumLength(200).When(x => x.Address!.Line1 is not null);
            RuleFor(x => x.Address!.Line2).MaximumLength(200).When(x => x.Address!.Line2 is not null);
            RuleFor(x => x.Address!.City).MaximumLength(100).When(x => x.Address!.City is not null);
            RuleFor(x => x.Address!.Region).MaximumLength(100).When(x => x.Address!.Region is not null);
            RuleFor(x => x.Address!.PostalCode).MaximumLength(20).When(x => x.Address!.PostalCode is not null);
            RuleFor(x => x.Address!.CountryCode).Length(2).When(x => x.Address!.CountryCode is not null);
        });
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
