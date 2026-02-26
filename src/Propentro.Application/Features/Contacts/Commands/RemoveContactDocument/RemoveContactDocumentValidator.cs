using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.RemoveContactDocument;

public sealed partial class RemoveContactDocumentValidator : AbstractValidator<RemoveContactDocumentCommand>
{
    public RemoveContactDocumentValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .Must(id => DocumentIdRegex().IsMatch(id))
            .WithMessage("Invalid document ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();

    [GeneratedRegex(@"^doc_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex DocumentIdRegex();
}
