using Dapper;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Parties;

namespace Propentro.Application.Parties.Queries.GetPartyById;

public sealed class GetPartyByIdHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetPartyByIdQuery, PartyDetailDto>
{
    public async Task<Result<PartyDetailDto>> Handle(
        GetPartyByIdQuery request,
        CancellationToken ct)
    {
        using var connection = db.CreateConnection();

        const string sql = """
            SELECT
                p.public_id AS PublicId,
                p.kind AS Kind,
                p.display_name AS DisplayName,
                p.first_name AS FirstName,
                p.last_name AS LastName,
                p.legal_name AS LegalName,
                p.trading_name AS TradingName,
                p.email AS Email,
                p.phone AS Phone,
                p.tax_number AS TaxNumber,
                p.tax_country AS TaxCountry,
                p.note AS Note,
                p.created_at AS CreatedAt,
                p.updated_at AS UpdatedAt
            FROM parties p
            WHERE p.public_id = @PublicId
              AND p.organization_id = @OrganizationId
              AND p.is_active = TRUE
            """;

        var party = await connection.QueryFirstOrDefaultAsync<PartyDetailDto>(
            sql,
            new { request.PublicId, currentUser.OrganizationId });

        if (party is null)
        {
            return PartyErrors.NotFoundById(request.PublicId);
        }

        return party;
    }
}
