using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Documents;

public sealed class Document : OrganizationEntity
{
    public const string PublicIdPrefix = "doc";

    public string Name { get; set; } = default!;
    public string DocumentType { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public Guid? UploadedById { get; set; }

    // Navigation
    public DocumentVersion? CurrentVersion { get; set; }
    public ICollection<DocumentVersion> Versions { get; set; } = [];

    private Document() { }

    public Document(string name, string documentType) : base(PublicIdPrefix)
    {
        Name = name;
        DocumentType = documentType;
    }
}
