using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;

namespace Propentro.Application.Parties.Commands.CreateParty;

public sealed class CreatePartyHandler(
    IAppDbContext db,
    ICurrentUser currentUser) : ICommandHandler<CreatePartyCommand, string>
{
    public async Task<Result<string>> Handle(CreatePartyCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.Email))
        {
            var emailExists = await db.Parties
                .AnyAsync(p => p.Email == request.Email, ct);

            if (emailExists)
            {
                return PartyErrors.EmailAlreadyExists;
            }
        }

        var party = new Party
        {
            Kind = request.Kind,
            DisplayName = request.DisplayName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            LegalName = request.LegalName,
            TradingName = request.TradingName,
            Email = request.Email,
            Phone = request.Phone,
            TaxNumber = request.TaxNumber,
            TaxCountry = request.TaxCountry,
            Note = request.Note
        };

        db.Parties.Add(party);
        await db.SaveChangesAsync(ct);

        return party.PublicId;
    }
}
