using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Documents;

namespace Propentro.Application.Features.Contacts.Queries.GetContactDocuments;

public sealed class GetContactDocumentsHandler(
    IDbConnectionFactory db,
    IUserContext currentUser,
    IBlobStorageService blobStorage) : IQueryHandler<GetContactDocumentsQuery, IReadOnlyList<ContactDocumentResponse>>
{
    public async Task<Result<IReadOnlyList<ContactDocumentResponse>>> Handle(
        GetContactDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                d.public_id AS document_id,
                d.name,
                d.document_type,
                d.description,
                cd.is_primary,
                dv.original_name AS original_file_name,
                dv.mime_type,
                dv.file_size,
                dv.storage_key,
                cd.created_at
            FROM contact_documents cd
            JOIN contacts c ON c.id = cd.contact_id
            JOIN documents d ON d.id = cd.document_id AND d.is_active = true
            LEFT JOIN document_versions dv ON dv.id = d.current_version_id
            WHERE c.public_id = @ContactId
              AND c.organization_id = @OrganizationId
              AND c.is_active = true
            ORDER BY cd.created_at DESC
            """;

        var rows = (await connection.QueryAsync<DocumentQueryResult>(
            sql,
            new { request.ContactId, currentUser.OrganizationId }))
            .ToList();

        var results = new List<ContactDocumentResponse>(rows.Count);
        foreach (var row in rows)
        {
            string? downloadUrl = null;
            if (row.StorageKey is not null)
            {
                downloadUrl = await blobStorage.GetPresignedDownloadUrlAsync(
                    row.StorageKey,
                    TimeSpan.FromMinutes(15),
                    cancellationToken);
            }

            results.Add(new ContactDocumentResponse(
                row.DocumentId,
                row.Name,
                row.DocumentType,
                row.Description,
                row.IsPrimary,
                row.OriginalFileName ?? "",
                row.MimeType ?? "",
                row.FileSize,
                downloadUrl,
                row.CreatedAt));
        }

        return results;
    }

    private sealed class DocumentQueryResult
    {
        public string DocumentId { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string DocumentType { get; init; } = null!;
        public string? Description { get; init; }
        public bool IsPrimary { get; init; }
        public string? OriginalFileName { get; init; }
        public string? MimeType { get; init; }
        public long? FileSize { get; init; }
        public string? StorageKey { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
