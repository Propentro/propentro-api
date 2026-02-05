using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Parties.Commands.UpdateParty;

public sealed class UpdatePartyHandler(
    IAppDbContext db) : ICommandHandler<UpdatePartyCommand>
{
    public async Task<Result> Handle(UpdatePartyCommand request, CancellationToken cancellationToken)
    {
        var party = await db.Parties
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, cancellationToken);

        if (party is null)
        {
            return PartyErrors.NotFoundById(request.PublicId);
        }

        if (request.DisplayName is not null) { party.DisplayName = request.DisplayName; }
        if (request.FirstName is not null) { party.FirstName = request.FirstName; }
        if (request.LastName is not null) { party.LastName = request.LastName; }
        if (request.LegalName is not null) { party.LegalName = request.LegalName; }
        if (request.TradingName is not null) { party.TradingName = request.TradingName; }
        if (request.Email is not null) { party.Email = request.Email; }
        if (request.Phone is not null) { party.Phone = request.Phone; }
        if (request.TaxNumber is not null) { party.TaxNumber = request.TaxNumber; }
        if (request.TaxCountry is not null) { party.TaxCountry = request.TaxCountry; }
        if (request.Note is not null) { party.Note = request.Note; }

        return Result.Success();
    }
}
