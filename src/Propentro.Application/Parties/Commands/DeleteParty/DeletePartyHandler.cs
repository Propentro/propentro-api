using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Parties.Commands.DeleteParty;

public sealed class DeletePartyHandler(
    IAppDbContext db) : ICommandHandler<DeletePartyCommand>
{
    public async Task<Result> Handle(DeletePartyCommand request, CancellationToken ct)
    {
        var party = await db.Parties
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, ct);

        if (party is null)
        {
            return PartyErrors.NotFoundById(request.PublicId);
        }

        party.IsActive = false;
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
