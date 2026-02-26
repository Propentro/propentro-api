using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public sealed class CreateContactHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<CreateContactCommand, string>
{
    public async Task<Result<string>> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        var displayName = ComputeDisplayName(request);
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

        dbContext.Contacts.Add(contact);

        // Address (optional)
        if (request.Address is not null)
        {
            var address = new Address
            {
                Id = Guid.CreateVersion7(),
                Line1 = request.Address.Line1,
                Line2 = request.Address.Line2,
                City = request.Address.City,
                Region = request.Address.Region,
                PostalCode = request.Address.PostalCode,
                CountryCode = request.Address.CountryCode
            };
            dbContext.Addresses.Add(address);
            contact.AddressId = address.Id;
        }

        // Roles
        foreach (var role in request.Roles)
        {
            contact.ContactRoles.Add(new ContactRole
            {
                ContactId = contact.Id,
                Role = role,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Activity log
        var activity = new Activity("contact", contact.Id, "system_event")
        {
            Title = "Contact created"
        };
        dbContext.Activities.Add(activity);

        // Domain event (outbox)
        contact.MarkAsCreated(currentUser.OrganizationId);

        // Cache invalidation
        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return contact.PublicId;
    }

    private static string ComputeDisplayName(CreateContactCommand request) =>
        request.Kind == "company"
            ? request.TradingName ?? request.LegalName ?? ""
            : $"{request.FirstName} {request.LastName}".Trim();
}
