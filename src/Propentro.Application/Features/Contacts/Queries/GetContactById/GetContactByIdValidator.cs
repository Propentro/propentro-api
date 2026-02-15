using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Queries.GetContactById;

public sealed partial class GetContactByIdValidator : AbstractValidator<GetContactByIdQuery>
{
    public GetContactByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format. Expected format: cnt_xxxxxxxxxxxx (cnt_ followed by 12 hex characters).");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
