using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Parties.Commands.DeleteParty;

public sealed class DeletePartyHandler(
    IAppDbContext db) : ICommandHandler<DeletePartyCommand>
{
    public async Task<Result> Handle(DeletePartyCommand request, CancellationToken cancellationToken)
    {
        var party = await db.Parties
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, cancellationToken);

        if (party is null)
        {
            return PartyErrors.NotFoundById(request.PublicId);
        }

        party.IsActive = false;

        return Result.Success();
    }
}
