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
            whereClause.Append("""
                 AND (
                    c.display_name ILIKE @Search
                    OR c.first_name ILIKE @Search
                    OR c.last_name ILIKE @Search
                    OR c.trading_name ILIKE @Search
                    OR c.email ILIKE @Search
                    OR c.phone ILIKE @Search
                )
                """);
            parameters.Add("Search", $"%{request.Search}%");
        }

        if (request.IsArchived.HasValue)
        {
            whereClause.Append(" AND c.is_archived = @IsArchived");
            parameters.Add("IsArchived", request.IsArchived.Value);
        }

        var countSql = $"SELECT COUNT(1) FROM contacts c {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (request.Page - 1) * request.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        var dataSql = $"""
            SELECT
                c.public_id AS Id,
                c.kind AS Kind,
                c.display_name AS DisplayName,
                c.first_name AS FirstName,
                c.last_name AS LastName,
                c.legal_name AS LegalName,
                c.trading_name AS TradingName,
                c.email AS Email,
                c.phone AS Phone,
                c.secondary_email AS SecondaryEmail,
                c.secondary_phone AS SecondaryPhone,
                c.trade AS Trade,
                c.emergency_contact_name AS EmergencyContactName,
                c.emergency_contact_phone AS EmergencyContactPhone,
                c.trade_other AS TradeOther,
                c.tax_number AS TaxNumber,
                c.tax_country AS TaxCountry,
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

        var contactIds = contacts.Select(c => c.Id).ToList();

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
            c.Id,
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
            c.TaxCountry,
            c.IsArchived,
            allRoles.GetValueOrDefault(c.Id, []),
            c.CreatedAt)).ToList();

        return new PagedResult<ContactListItemResponse>(items, totalCount, request.Page, request.PageSize);
    }

    private sealed class ContactListItemQueryResult
    {
        public string Id { get; init; } = null!;
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
        public string? TaxCountry { get; init; }
        public bool IsArchived { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    private sealed class ContactRoleRow
    {
        public string ContactId { get; init; } = null!;
        public string Role { get; init; } = null!;
    }
}
