using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Parties;

namespace Propentro.Application.Parties.Queries.GetParties;

public sealed class GetPartiesHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetPartiesQuery, PagedResult<PartyDto>>
{
    public async Task<Result<PagedResult<PartyDto>>> Handle(
        GetPartiesQuery request,
        CancellationToken ct)
    {
        using var connection = db.CreateConnection();

        var offset = (request.Page - 1) * request.PageSize;

        const string countSql = """
            SELECT COUNT(*)
            FROM parties p
            WHERE p.organization_id = @OrganizationId
              AND p.is_active = TRUE
              AND (@Search IS NULL OR p.display_name ILIKE '%' || @Search || '%')
              AND (@Kind IS NULL OR p.kind = @Kind)
            """;

        const string dataSql = """
            SELECT
                p.public_id AS PublicId,
                p.kind AS Kind,
                p.display_name AS DisplayName,
                p.first_name AS FirstName,
                p.last_name AS LastName,
                p.legal_name AS LegalName,
                p.email AS Email,
                p.phone AS Phone,
                p.created_at AS CreatedAt
            FROM parties p
            WHERE p.organization_id = @OrganizationId
              AND p.is_active = TRUE
              AND (@Search IS NULL OR p.display_name ILIKE '%' || @Search || '%')
              AND (@Kind IS NULL OR p.kind = @Kind)
            ORDER BY p.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        var parameters = new
        {
            OrganizationId = currentUser.OrganizationId,
            request.Search,
            request.Kind,
            request.PageSize,
            Offset = offset
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await connection.QueryAsync<PartyDto>(dataSql, parameters);

        return new PagedResult<PartyDto>(
            items.ToList(),
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}
