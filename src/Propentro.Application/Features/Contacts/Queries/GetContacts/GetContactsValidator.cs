using FluentValidation;

namespace Propentro.Application.Features.Contacts.Queries.GetContacts;

public sealed class GetContactsValidator : AbstractValidator<GetContactsQuery>
{
    private static readonly string[] _validRoles = ["tenant", "owner", "supplier", "staff"];

    public GetContactsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Role)
            .Must(r => _validRoles.Contains(r))
            .When(x => x.Role is not null)
            .WithMessage("Invalid role filter. Valid roles: tenant, owner, supplier, staff.");
    }
}
