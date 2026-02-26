using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.UpdateContact;

public sealed class UpdateContactHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<UpdateContactCommand>
{
    public async Task<Result> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        // Update fields (null = no change)
        if (request.FirstName is not null) contact.FirstName = request.FirstName;
        if (request.LastName is not null) contact.LastName = request.LastName;
        if (request.LegalName is not null) contact.LegalName = request.LegalName;
        if (request.TradingName is not null) contact.TradingName = request.TradingName;
        if (request.Email is not null) contact.Email = request.Email;
        if (request.Phone is not null) contact.Phone = request.Phone;
        if (request.SecondaryEmail is not null) contact.SecondaryEmail = request.SecondaryEmail;
        if (request.SecondaryPhone is not null) contact.SecondaryPhone = request.SecondaryPhone;
        if (request.EmergencyContactName is not null) contact.EmergencyContactName = request.EmergencyContactName;
        if (request.EmergencyContactPhone is not null) contact.EmergencyContactPhone = request.EmergencyContactPhone;
        if (request.Trade is not null) contact.Trade = request.Trade;
        if (request.TradeOther is not null) contact.TradeOther = request.TradeOther;
        if (request.TaxNumber is not null) contact.TaxNumber = request.TaxNumber;
        if (request.TaxCountry is not null) contact.TaxCountry = request.TaxCountry;
        if (request.Note is not null) contact.Note = request.Note;

        // Recompute display name
        contact.DisplayName = contact.Kind == "company"
            ? contact.TradingName ?? contact.LegalName ?? ""
            : $"{contact.FirstName} {contact.LastName}".Trim();

        // Address handling
        if (request.Address is not null)
        {
            if (contact.Address is not null)
            {
                if (request.Address.Line1 is not null) contact.Address.Line1 = request.Address.Line1;
                if (request.Address.Line2 is not null) contact.Address.Line2 = request.Address.Line2;
                if (request.Address.City is not null) contact.Address.City = request.Address.City;
                if (request.Address.Region is not null) contact.Address.Region = request.Address.Region;
                if (request.Address.PostalCode is not null) contact.Address.PostalCode = request.Address.PostalCode;
                if (request.Address.CountryCode is not null) contact.Address.CountryCode = request.Address.CountryCode;
            }
            else
            {
                var address = new Address
                {
                    Id = Guid.CreateVersion7(),
                    Line1 = request.Address.Line1 ?? "",
                    Line2 = request.Address.Line2,
                    City = request.Address.City ?? "",
                    Region = request.Address.Region,
                    PostalCode = request.Address.PostalCode,
                    CountryCode = request.Address.CountryCode ?? ""
                };
                dbContext.Addresses.Add(address);
                contact.AddressId = address.Id;
            }
        }

        // Activity log
        var activity = new Activity("contact", contact.Id, "system_event")
        {
            Title = "Contact updated",
            Description = BuildChangeDescription(request)
        };
        dbContext.Activities.Add(activity);

        // Domain event (outbox)
        contact.MarkAsUpdated(currentUser.OrganizationId);

        // Cache invalidation
        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }

    private static string? BuildChangeDescription(UpdateContactCommand request)
    {
        List<string> fields = [];

        if (request.FirstName is not null) fields.Add("first name");
        if (request.LastName is not null) fields.Add("last name");
        if (request.LegalName is not null) fields.Add("legal name");
        if (request.TradingName is not null) fields.Add("trading name");
        if (request.Email is not null) fields.Add("email");
        if (request.Phone is not null) fields.Add("phone");
        if (request.SecondaryEmail is not null) fields.Add("secondary email");
        if (request.SecondaryPhone is not null) fields.Add("secondary phone");
        if (request.EmergencyContactName is not null) fields.Add("emergency contact name");
        if (request.EmergencyContactPhone is not null) fields.Add("emergency contact phone");
        if (request.Trade is not null) fields.Add("trade");
        if (request.TaxNumber is not null) fields.Add("tax number");
        if (request.TaxCountry is not null) fields.Add("tax country");
        if (request.Note is not null) fields.Add("note");
        if (request.Address is not null) fields.Add("address");

        return fields.Count > 0 ? $"Updated {string.Join(", ", fields)}" : null;
    }
}
