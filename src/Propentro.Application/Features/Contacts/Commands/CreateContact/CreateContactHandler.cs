using Dapper;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public sealed class CreateContactHandler(
    IAppDbContext dbContext,
    IDbConnectionFactory db,
    ICurrentUser currentUser) : ICommandHandler<CreateContactCommand, string>
{
    public async Task<Result<string>> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await using var connection = db.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            const string duplicateCheckSql = """
                SELECT COUNT(1)
                FROM contacts
                WHERE email = @Email
                  AND organization_id = @OrganizationId
                  AND is_active = true
                """;

            var existingCount = await connection.ExecuteScalarAsync<int>(
                duplicateCheckSql,
                new { request.Email, currentUser.OrganizationId });

            if (existingCount > 0)
                return ContactErrors.DuplicateEmail;
        }

        var displayName = ComputeDisplayName(request.Kind, request.FirstName, request.LastName, request.TradingName, request.LegalName);

        var contact = new Contact(displayName, request.Kind)
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            LegalName = request.LegalName,
            TradingName = request.TradingName,
            Email = request.Email,
            Phone = request.Phone,
            SecondaryEmail = request.SecondaryEmail,
            SecondaryPhone = request.SecondaryPhone,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Trade = request.Trade,
            TradeOther = request.TradeOther,
            TaxNumber = request.TaxNumber,
            TaxCountry = request.TaxCountry,
            Note = request.Note
        };

        if (request.Address is not null)
        {
            var address = new Address
            {
                Line1 = request.Address.Line1,
                Line2 = request.Address.Line2,
                City = request.Address.City,
                Region = request.Address.Region,
                PostalCode = request.Address.PostalCode,
                CountryCode = request.Address.CountryCode
            };

            dbContext.Addresses.Add(address);
            contact.Address = address;
        }

        foreach (var role in request.Roles)
        {
            contact.ContactRoles.Add(new ContactRole
            {
                ContactId = contact.Id,
                Role = role
            });
        }

        dbContext.Contacts.Add(contact);

        dbContext.Activities.Add(new Activity("contact", contact.Id, "system_event")
        {
            Title = "Contact created"
        });

        return contact.PublicId;
    }

    internal static string ComputeDisplayName(string kind, string? firstName, string? lastName, string? tradingName, string? legalName)
    {
        if (kind is "person")
        {
            var parts = new[] { firstName, lastName }.Where(p => !string.IsNullOrWhiteSpace(p));
            var name = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(name) ? "Unnamed Contact" : name;
        }

        // Treat blank/whitespace tradingName as missing so we fall through to legalName
        if (!string.IsNullOrWhiteSpace(tradingName))
            return tradingName;

        if (!string.IsNullOrWhiteSpace(legalName))
            return legalName;

        return "Unnamed Organization";
    }
}
