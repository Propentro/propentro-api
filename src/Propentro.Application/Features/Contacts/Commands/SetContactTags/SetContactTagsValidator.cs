using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.SetContactTags;

public sealed partial class SetContactTagsValidator : AbstractValidator<SetContactTagsCommand>
{
    public SetContactTagsValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.TagIds)
            .NotNull()
            .Must(ids => ids.Length <= 50)
            .WithMessage("Cannot set more than 50 tags at once.");

        RuleForEach(x => x.TagIds)
            .NotEmpty()
            .Must(id => TagIdRegex().IsMatch(id))
            .WithMessage("Invalid tag ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();

    [GeneratedRegex(@"^tag_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex TagIdRegex();
}
