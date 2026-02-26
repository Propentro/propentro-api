using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.InitiateContactDocumentUpload;

public sealed partial class InitiateContactDocumentUploadValidator
    : AbstractValidator<InitiateContactDocumentUploadCommand>
{
    private const long _maxFileSize = 10 * 1024 * 1024; // 10 MB

    private static readonly string[] _allowedDocumentTypes =
    [
        "photo", "contract", "certificate", "invoice", "id_document", "floor_plan", "other"
    ];

    private static readonly string[] _allowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ];

    public InitiateContactDocumentUploadValidator()
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
            .Must(dt => _allowedDocumentTypes.Contains(dt))
            .WithMessage("Invalid document type. Allowed: photo, contract, certificate, invoice, id_document, floor_plan, other.");

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => _allowedContentTypes.Contains(ct))
            .WithMessage("Unsupported content type.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(_maxFileSize)
            .WithMessage($"File size must not exceed {_maxFileSize / (1024 * 1024)} MB.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
