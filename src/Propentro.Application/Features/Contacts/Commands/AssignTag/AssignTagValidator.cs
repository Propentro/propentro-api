using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.AssignTag;

public sealed partial class AssignTagValidator : AbstractValidator<AssignTagCommand>
{
    public AssignTagValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.TagId)
            .NotEmpty()
            .Must(id => TagIdRegex().IsMatch(id))
            .WithMessage("Invalid tag ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();

    [GeneratedRegex(@"^tag_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex TagIdRegex();
}
