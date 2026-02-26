using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.AddContactDocument;

public sealed partial class AddContactDocumentValidator : AbstractValidator<AddContactDocumentCommand>
{
    private const long _maxFileSize = 10 * 1024 * 1024; // 10 MB

    public AddContactDocumentValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.DocumentType)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.FileName)
            .NotEmpty();

        RuleFor(x => x.ContentType)
            .NotEmpty();

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(_maxFileSize)
            .WithMessage($"File size must not exceed {_maxFileSize / (1024 * 1024)} MB.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
