using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Contacts;
using Propentro.Contracts.Tags;

namespace Propentro.Application.Features.Contacts.Queries.GetContactById;

public sealed class GetContactByIdHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetContactByIdQuery, ContactResponse>
{
    public async Task<Result<ContactResponse>> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string contactSql = """
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
                c.emergency_contact_name,
                c.emergency_contact_phone,
                c.trade,
                c.trade_other,
                c.tax_number,
                tc.name AS tax_country_name,
                c.note,
                c.is_archived,
                c.archived_at,
                ab.first_name || ' ' || ab.last_name AS archived_by_name,
                lu.public_id AS user_public_id,
                c.created_at,
                c.updated_at,
                cb.first_name || ' ' || cb.last_name AS created_by_name,
                ub.first_name || ' ' || ub.last_name AS updated_by_name,
                a.line1 AS address_line1,
                a.line2 AS address_line2,
                a.city AS address_city,
                a.region AS address_region,
                a.postal_code AS address_postal_code,
                ac.name AS address_country_name
            FROM contacts c
            LEFT JOIN addresses a ON a.id = c.address_id
            LEFT JOIN users cb ON cb.id = c.created_by_id
            LEFT JOIN users ub ON ub.id = c.updated_by_id
            LEFT JOIN users ab ON ab.id = c.archived_by_id
            LEFT JOIN users lu ON lu.id = c.user_id
            LEFT JOIN countries tc ON tc.code = c.tax_country
            LEFT JOIN countries ac ON ac.code = a.country_code
            WHERE c.public_id = @Id
              AND c.organization_id = @OrganizationId
              AND c.is_active = true
            """;

        var contact = await connection.QueryFirstOrDefaultAsync<ContactQueryResult>(
            contactSql,
            new { request.Id, currentUser.OrganizationId });

        if (contact is null)
            return ContactErrors.NotFoundById(request.Id);

        const string rolesSql = """
            SELECT cr.role
            FROM contact_roles cr
            JOIN contacts c ON c.id = cr.contact_id
            WHERE c.public_id = @Id
              AND c.organization_id = @OrganizationId
            """;

        var roles = (await connection.QueryAsync<string>(
            rolesSql,
            new { request.Id, currentUser.OrganizationId }))
            .ToList();

        const string tagsSql = """
            SELECT t.public_id AS id, t.name, t.color, t.created_at
            FROM entity_tags et
            JOIN tags t ON t.id = et.tag_id AND t.is_active = true
            JOIN contacts c ON c.id = et.entity_id
            WHERE et.entity_type = 'contact'
              AND c.public_id = @Id
              AND c.organization_id = @OrganizationId
            ORDER BY t.name
            """;

        var tags = (await connection.QueryAsync<TagQueryResult>(
            tagsSql,
            new { request.Id, currentUser.OrganizationId }))
            .Select(t => new TagResponse(t.Id, t.Name, t.Color, t.CreatedAt))
            .ToList();

        ContactAddressResponse? address = null;
        if (contact.AddressLine1 is not null)
        {
            address = new ContactAddressResponse(
                contact.AddressLine1,
                contact.AddressLine2,
                contact.AddressCity!,
                contact.AddressRegion,
                contact.AddressPostalCode,
                contact.AddressCountryName ?? "");
        }

        var response = new ContactResponse(
            contact.PublicId,
            contact.Kind,
            contact.DisplayName,
            contact.FirstName,
            contact.LastName,
            contact.LegalName,
            contact.TradingName,
            contact.Email,
            contact.Phone,
            contact.SecondaryEmail,
            contact.SecondaryPhone,
            contact.EmergencyContactName,
            contact.EmergencyContactPhone,
            contact.Trade,
            contact.TradeOther,
            contact.TaxNumber,
            contact.TaxCountryName,
            contact.Note,
            contact.IsArchived,
            contact.ArchivedAt,
            contact.ArchivedByName,
            contact.UserPublicId,
            roles,
            tags,
            address,
            contact.CreatedByName,
            contact.UpdatedByName,
            contact.CreatedAt,
            contact.UpdatedAt);

        return response;
    }

    private sealed class ContactQueryResult
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
        public string? EmergencyContactName { get; init; }
        public string? EmergencyContactPhone { get; init; }
        public string? Trade { get; init; }
        public string? TradeOther { get; init; }
        public string? TaxNumber { get; init; }
        public string? TaxCountryName { get; init; }
        public string? Note { get; init; }
        public bool IsArchived { get; init; }
        public DateTimeOffset? ArchivedAt { get; init; }
        public string? ArchivedByName { get; init; }
        public string? UserPublicId { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public string? CreatedByName { get; init; }
        public string? UpdatedByName { get; init; }
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? AddressCity { get; init; }
        public string? AddressRegion { get; init; }
        public string? AddressPostalCode { get; init; }
        public string? AddressCountryName { get; init; }
    }

    private sealed class TagQueryResult
    {
        public string Id { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? Color { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
