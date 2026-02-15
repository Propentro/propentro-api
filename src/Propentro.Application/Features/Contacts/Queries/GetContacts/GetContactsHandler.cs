using System.Text;
using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Queries.GetContacts;

public sealed class GetContactsHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetContactsQuery, PagedResult<ContactListItemResponse>>
{
    public async Task<Result<PagedResult<ContactListItemResponse>>> Handle(
        GetContactsQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var whereClause = new StringBuilder("WHERE c.organization_id = @OrganizationId AND c.is_active = true");
        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", currentUser.OrganizationId);

        if (request.Role is not null)
        {
            whereClause.Append("""
                 AND EXISTS (
                    SELECT 1 FROM contact_roles cr
                    WHERE cr.contact_id = c.id AND cr.role = @Role
                )
                """);
            parameters.Add("Role", request.Role);
        }

        if (request.Search is not null)
        {
            whereClause.Append(" AND (c.display_name ILIKE @Search OR c.email ILIKE @Search OR c.phone ILIKE @Search)");
            parameters.Add("Search", $"%{request.Search}%");
        }

        var isArchived = request.IsArchived ?? false;
        whereClause.Append(" AND c.is_archived = @IsArchived");
        parameters.Add("IsArchived", isArchived);

        var countSql = $"SELECT COUNT(1) FROM contacts c {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (request.Page - 1) * request.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        var dataSql = $"""
            SELECT
                c.public_id AS ContactId,
                c.kind AS Kind,
                c.display_name AS DisplayName,
                c.email AS Email,
                c.phone AS Phone,
                c.trade AS Trade,
                c.is_archived AS IsArchived,
                c.created_at AS CreatedAt
            FROM contacts c
            {whereClause}
            ORDER BY c.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var contacts = (await connection.QueryAsync<ContactListItemQueryResult>(dataSql, parameters)).ToList();

        if (contacts.Count == 0)
            return new PagedResult<ContactListItemResponse>([], totalCount, request.Page, request.PageSize);

        var contactIds = contacts.Select(c => c.ContactId).ToList();

        const string rolesSql = """
            SELECT c.public_id AS ContactId, cr.role AS Role
            FROM contact_roles cr
            JOIN contacts c ON c.id = cr.contact_id
            WHERE c.public_id = ANY(@ContactIds)
              AND c.organization_id = @OrganizationId
              AND c.is_active = true
            """;

        var allRoles = (await connection.QueryAsync<ContactRoleRow>(
            rolesSql,
            new { ContactIds = contactIds.ToArray(), currentUser.OrganizationId }))
            .GroupBy(r => r.ContactId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.Role).Order().ToList());

        var items = contacts.Select(c => new ContactListItemResponse(
            c.ContactId,
            c.Kind,
            c.DisplayName,
            c.Email,
            c.Phone,
            c.Trade,
            c.IsArchived,
            allRoles.GetValueOrDefault(c.ContactId, []),
            c.CreatedAt)).ToList();

        return new PagedResult<ContactListItemResponse>(items, totalCount, request.Page, request.PageSize);
    }

    private sealed record ContactListItemQueryResult(
        string ContactId,
        string Kind,
        string DisplayName,
        string? Email,
        string? Phone,
        string? Trade,
        bool IsArchived,
        DateTime CreatedAt);

    private sealed record ContactRoleRow(string ContactId, string Role);
}
