using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Units.Commands.DeleteUnit;

public sealed class DeleteUnitHandler(
    IAppDbContext db) : ICommandHandler<DeleteUnitCommand>
{
    public async Task<Result> Handle(DeleteUnitCommand request, CancellationToken ct)
    {
        var unit = await db.Units
            .FirstOrDefaultAsync(u => u.PublicId == request.PublicId, ct);

        if (unit is null)
        {
            return UnitErrors.NotFoundById(request.PublicId);
        }

        unit.IsActive = false;
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
