using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public sealed class CreateContactValidator : AbstractValidator<CreateContactCommand>
{
    private static readonly string[] _validKinds = ["person", "organization"];
    private static readonly string[] _validRoles = ["tenant", "owner", "supplier"];

    public CreateContactValidator()
    {
        RuleFor(x => x.Kind)
            .NotEmpty()
            .Must(k => _validKinds.Contains(k))
            .WithMessage("Kind must be 'person' or 'organization'.");

        RuleFor(x => x.Roles)
            .NotEmpty()
            .WithMessage("At least one role is required.");

        RuleForEach(x => x.Roles)
            .Must(r => _validRoles.Contains(r))
            .WithMessage("Each role must be one of: tenant, owner, supplier.");

        RuleFor(x => x.Roles)
            .Must(roles => roles.Distinct().Count() == roles.Length)
            .WithMessage("Duplicate roles are not allowed.");

        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.LegalName).MaximumLength(200);
        RuleFor(x => x.TradingName).MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.SecondaryEmail).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.SecondaryEmail));
        RuleFor(x => x.SecondaryPhone).MaximumLength(50);
        RuleFor(x => x.EmergencyContactName).MaximumLength(200);
        RuleFor(x => x.EmergencyContactPhone).MaximumLength(50);
        RuleFor(x => x.Trade).MaximumLength(100);
        RuleFor(x => x.TradeOther).MaximumLength(100);
        RuleFor(x => x.TaxNumber).MaximumLength(50);
        RuleFor(x => x.TaxCountry).MaximumLength(10);
        RuleFor(x => x.Note).MaximumLength(2000);

        When(x => x.Address is not null, () =>
        {
            RuleFor(x => x.Address!.Line1).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Address!.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Address!.CountryCode).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Address!.Line2).MaximumLength(200);
            RuleFor(x => x.Address!.Region).MaximumLength(100);
            RuleFor(x => x.Address!.PostalCode).MaximumLength(20);
        });
    }
}
