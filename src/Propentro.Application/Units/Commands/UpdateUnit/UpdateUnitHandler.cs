using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Units.Commands.UpdateUnit;

public sealed class UpdateUnitHandler(
    IAppDbContext db) : ICommandHandler<UpdateUnitCommand>
{
    public async Task<Result> Handle(UpdateUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await db.Units
            .FirstOrDefaultAsync(u => u.PublicId == request.PublicId, cancellationToken);

        if (unit is null)
        {
            return UnitErrors.NotFoundById(request.PublicId);
        }

        if (request.Name is not null) { unit.Name = request.Name; }
        if (request.UnitType is not null) { unit.UnitType = request.UnitType; }
        if (request.UsageType is not null) { unit.UsageType = request.UsageType; }
        if (request.RentalMode is not null) { unit.RentalMode = request.RentalMode; }
        if (request.Status is not null) { unit.Status = request.Status; }
        if (request.Floor.HasValue) { unit.Floor = request.Floor; }
        if (request.UnitNumber is not null) { unit.UnitNumber = request.UnitNumber; }
        if (request.Bedrooms.HasValue) { unit.Bedrooms = request.Bedrooms; }
        if (request.Bathrooms.HasValue) { unit.Bathrooms = request.Bathrooms; }
        if (request.AreaSqm.HasValue) { unit.AreaSqm = request.AreaSqm; }
        if (request.DefaultRentMinor.HasValue) { unit.DefaultRentMinor = request.DefaultRentMinor; }
        if (request.DefaultDepositMinor.HasValue) { unit.DefaultDepositMinor = request.DefaultDepositMinor; }
        if (request.DefaultCurrency is not null) { unit.DefaultCurrency = request.DefaultCurrency; }
        if (request.Note is not null) { unit.Note = request.Note; }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
