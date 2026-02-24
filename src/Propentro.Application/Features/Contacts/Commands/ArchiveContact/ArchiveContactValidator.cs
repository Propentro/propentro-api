using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.ArchiveContact;

public sealed partial class ArchiveContactValidator : AbstractValidator<ArchiveContactCommand>
{
    public ArchiveContactValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
