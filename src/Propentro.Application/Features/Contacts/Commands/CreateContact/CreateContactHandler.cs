using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;
using Activity = Propentro.Domain.Entities.Activity;

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

        var contact = new Contact(request.DisplayName, request.Kind)
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
                Role = role,
                CreatedAt = DateTime.UtcNow
            });
        }

        dbContext.Contacts.Add(contact);

        dbContext.Activities.Add(new Activity("contact", contact.Id, "system_event")
        {
            Title = "Contact created",
            OrganizationId = currentUser.OrganizationId,
            CreatedById = currentUser.UserId
        });

        return contact.PublicId;
    }
}
