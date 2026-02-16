using FluentValidation;

namespace Propentro.Application.Features.Contacts.Queries.GetContacts;

public sealed class GetContactsValidator : AbstractValidator<GetContactsQuery>
{
    private static readonly string[] _validRoles = ["tenant", "owner", "supplier"];

    public GetContactsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Role)
            .Must(r => _validRoles.Contains(r!))
            .When(x => x.Role is not null)
            .WithMessage("Role must be one of: tenant, owner, supplier.");
    }
}
