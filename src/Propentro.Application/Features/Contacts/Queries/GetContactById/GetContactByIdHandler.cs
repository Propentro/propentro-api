using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Queries.GetContactById;

public sealed class GetContactByIdHandler(
    IDbConnectionFactory db,
    ICurrentUser currentUser) : IQueryHandler<GetContactByIdQuery, ContactResponse>
{
    public async Task<Result<ContactResponse>> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string contactSql = """
            SELECT
                c.public_id AS ContactId,
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
                c.emergency_contact_name AS EmergencyContactName,
                c.emergency_contact_phone AS EmergencyContactPhone,
                c.trade AS Trade,
                c.trade_other AS TradeOther,
                c.tax_number AS TaxNumber,
                c.tax_country AS TaxCountry,
                c.note AS Note,
                c.is_archived AS IsArchived,
                c.created_at AS CreatedAt,
                c.updated_at AS UpdatedAt,
                CONCAT(cu.first_name, ' ', cu.last_name) AS CreatedByName,
                CONCAT(uu.first_name, ' ', uu.last_name) AS UpdatedByName,
                a.line1 AS AddressLine1,
                a.line2 AS AddressLine2,
                a.city AS AddressCity,
                a.region AS AddressRegion,
                a.postal_code AS AddressPostalCode,
                a.country_code AS AddressCountryCode
            FROM contacts c
            LEFT JOIN addresses a ON a.id = c.address_id AND a.is_active = true
            LEFT JOIN users cu ON cu.id = c.created_by_id AND cu.is_active = true
            LEFT JOIN users uu ON uu.id = c.updated_by_id AND uu.is_active = true
            WHERE c.public_id = @Id
              AND c.organization_id = @OrganizationId
              AND c.is_active = true
            """;

        var contactResult = await connection.QueryFirstOrDefaultAsync<ContactQueryResult>(
            contactSql,
            new { request.Id, currentUser.OrganizationId });

        if (contactResult is null)
            return ContactErrors.NotFoundById(request.Id);

        const string rolesSql = """
            SELECT cr.role
            FROM contact_roles cr
            JOIN contacts c ON c.id = cr.contact_id
            WHERE c.public_id = @Id
              AND c.organization_id = @OrganizationId
              AND c.is_active = true
            """;

        var roles = (await connection.QueryAsync<string>(
            rolesSql,
            new { request.Id, currentUser.OrganizationId }))
            .Order().ToList();

        ContactAddressResponse? address = null;
        if (contactResult.AddressLine1 is not null)
        {
            address = new ContactAddressResponse(
                contactResult.AddressLine1,
                contactResult.AddressLine2,
                contactResult.AddressCity!,
                contactResult.AddressRegion,
                contactResult.AddressPostalCode,
                contactResult.AddressCountryCode!);
        }

        return new ContactResponse(
            contactResult.ContactId,
            contactResult.Kind,
            contactResult.DisplayName,
            contactResult.FirstName,
            contactResult.LastName,
            contactResult.LegalName,
            contactResult.TradingName,
            contactResult.Email,
            contactResult.Phone,
            contactResult.SecondaryEmail,
            contactResult.SecondaryPhone,
            contactResult.EmergencyContactName,
            contactResult.EmergencyContactPhone,
            contactResult.Trade,
            contactResult.TradeOther,
            contactResult.TaxNumber,
            contactResult.TaxCountry,
            contactResult.Note,
            contactResult.IsArchived,
            roles,
            address,
            contactResult.CreatedByName,
            contactResult.UpdatedByName,
            contactResult.CreatedAt,
            contactResult.UpdatedAt);
    }

    private sealed record ContactQueryResult(
        string ContactId,
        string Kind,
        string DisplayName,
        string? FirstName,
        string? LastName,
        string? LegalName,
        string? TradingName,
        string? Email,
        string? Phone,
        string? SecondaryEmail,
        string? SecondaryPhone,
        string? EmergencyContactName,
        string? EmergencyContactPhone,
        string? Trade,
        string? TradeOther,
        string? TaxNumber,
        string? TaxCountry,
        string? Note,
        bool IsArchived,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        string? CreatedByName,
        string? UpdatedByName,
        string? AddressLine1,
        string? AddressLine2,
        string? AddressCity,
        string? AddressRegion,
        string? AddressPostalCode,
        string? AddressCountryCode);
}
