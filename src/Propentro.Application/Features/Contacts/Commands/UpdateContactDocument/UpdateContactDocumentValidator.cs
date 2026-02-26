using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.UpdateContactDocument;

public sealed partial class UpdateContactDocumentValidator : AbstractValidator<UpdateContactDocumentCommand>
{
    public UpdateContactDocumentValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .Must(id => DocumentIdRegex().IsMatch(id))
            .WithMessage("Invalid document ID format.");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null);
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();

    [GeneratedRegex(@"^doc_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex DocumentIdRegex();
}
