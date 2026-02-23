using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.DeleteContact;

public sealed partial class DeleteContactValidator : AbstractValidator<DeleteContactCommand>
{
    public DeleteContactValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
