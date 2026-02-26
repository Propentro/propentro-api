using System.Text;
using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Contacts;
using Propentro.Contracts.Tags;

namespace Propentro.Application.Features.Contacts.Queries.GetContacts;

public sealed class GetContactsHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetContactsQuery, PagedResult<ContactListItemResponse>>
{
    public async Task<Result<PagedResult<ContactListItemResponse>>> Handle(
        GetContactsQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", currentUser.OrganizationId);
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("Limit", request.PageSize);

        var whereBuilder = new StringBuilder("""
            WHERE c.organization_id = @OrganizationId
              AND c.is_active = true
            """);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            whereBuilder.Append("""

                  AND (c.display_name ILIKE @Search
                    OR c.email ILIKE @Search
                    OR c.phone ILIKE @Search)
                """);
            parameters.Add("Search", $"%{request.Search}%");
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            whereBuilder.Append("""

                  AND EXISTS (
                    SELECT 1 FROM contact_roles cr
                    WHERE cr.contact_id = c.id AND cr.role = @Role)
                """);
            parameters.Add("Role", request.Role);
        }

        var where = whereBuilder.ToString();

        var countSql = $"""
            SELECT COUNT(*)
            FROM contacts c
            {where}
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        var listSql = $"""
            SELECT
                c.public_id,
                c.kind,
                c.display_name,
                c.first_name,
                c.last_name,
                c.legal_name,
                c.trading_name,
                c.email,
                c.phone,
                c.secondary_email,
                c.secondary_phone,
                c.trade,
                c.emergency_contact_name,
                c.emergency_contact_phone,
                c.trade_other,
                c.tax_number,
                tc.name AS tax_country_name,
                lu.public_id AS user_public_id,
                c.is_archived,
                c.created_at
            FROM contacts c
            LEFT JOIN users lu ON lu.id = c.user_id
            LEFT JOIN countries tc ON tc.code = c.tax_country
            {where}
            ORDER BY c.created_at DESC
            OFFSET @Offset LIMIT @Limit
            """;

        var contacts = (await connection.QueryAsync<ContactListQueryResult>(listSql, parameters)).ToList();

        if (contacts.Count == 0)
        {
            return new PagedResult<ContactListItemResponse>([], total, request.Page, request.PageSize);
        }

        // Fetch roles for all contacts in one query
        var publicIds = contacts.Select(c => c.PublicId).ToArray();
        const string rolesSql = """
            SELECT c.public_id AS contact_public_id, cr.role
            FROM contact_roles cr
            JOIN contacts c ON c.id = cr.contact_id
            WHERE c.public_id = ANY(@PublicIds)
              AND c.organization_id = @OrganizationId
            """;

        var roleRows = (await connection.QueryAsync<ContactRoleRow>(
            rolesSql,
            new { PublicIds = publicIds, currentUser.OrganizationId }))
            .ToList();

        var rolesLookup = roleRows
            .GroupBy(r => r.ContactPublicId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.Role).ToList());

        // Fetch tags for all contacts in one query
        const string tagsSql = """
            SELECT c.public_id AS contact_public_id, t.public_id AS tag_id, t.name, t.color, t.created_at
            FROM entity_tags et
            JOIN tags t ON t.id = et.tag_id AND t.is_active = true
            JOIN contacts c ON c.id = et.entity_id
            WHERE et.entity_type = 'contact'
              AND c.public_id = ANY(@PublicIds)
              AND c.organization_id = @OrganizationId
            ORDER BY t.name
            """;

        var tagRows = (await connection.QueryAsync<ContactTagRow>(
            tagsSql,
            new { PublicIds = publicIds, currentUser.OrganizationId }))
            .ToList();

        var tagsLookup = tagRows
            .GroupBy(t => t.ContactPublicId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TagResponse>)g.Select(t => new TagResponse(t.TagId, t.Name, t.Color, t.CreatedAt)).ToList());

        var items = contacts.Select(c => new ContactListItemResponse(
            c.PublicId,
            c.Kind,
            c.DisplayName,
            c.FirstName,
            c.LastName,
            c.LegalName,
            c.TradingName,
            c.Email,
            c.Phone,
            c.SecondaryEmail,
            c.SecondaryPhone,
            c.Trade,
            c.EmergencyContactName,
            c.EmergencyContactPhone,
            c.TradeOther,
            c.TaxNumber,
            c.TaxCountryName,
            c.UserPublicId,
            c.IsArchived,
            rolesLookup.GetValueOrDefault(c.PublicId, []),
            tagsLookup.GetValueOrDefault(c.PublicId, []),
            c.CreatedAt))
            .ToList();

        return new PagedResult<ContactListItemResponse>(items, total, request.Page, request.PageSize);
    }

    private sealed class ContactListQueryResult
    {
        public string PublicId { get; init; } = null!;
        public string Kind { get; init; } = null!;
        public string DisplayName { get; init; } = null!;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? LegalName { get; init; }
        public string? TradingName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? SecondaryEmail { get; init; }
        public string? SecondaryPhone { get; init; }
        public string? Trade { get; init; }
        public string? EmergencyContactName { get; init; }
        public string? EmergencyContactPhone { get; init; }
        public string? TradeOther { get; init; }
        public string? TaxNumber { get; init; }
        public string? TaxCountryName { get; init; }
        public string? UserPublicId { get; init; }
        public bool IsArchived { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    private sealed class ContactRoleRow
    {
        public string ContactPublicId { get; init; } = null!;
        public string Role { get; init; } = null!;
    }

    private sealed class ContactTagRow
    {
        public string ContactPublicId { get; init; } = null!;
        public string TagId { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? Color { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
