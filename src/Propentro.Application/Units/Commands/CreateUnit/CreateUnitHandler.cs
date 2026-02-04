using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;

namespace Propentro.Application.Units.Commands.CreateUnit;

public sealed class CreateUnitHandler(
    IAppDbContext db) : ICommandHandler<CreateUnitCommand, string>
{
    public async Task<Result<string>> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        var propertyExists = await db.Properties
            .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);

        if (!propertyExists)
        {
            return UnitErrors.PropertyNotFound;
        }

        var unit = new Unit
        {
            PropertyId = request.PropertyId,
            Name = request.Name,
            UnitType = request.UnitType,
            UsageType = request.UsageType ?? "residential",
            RentalMode = request.RentalMode ?? "long_term",
            Floor = request.Floor,
            UnitNumber = request.UnitNumber,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            AreaSqm = request.AreaSqm,
            DefaultRentMinor = request.DefaultRentMinor,
            DefaultDepositMinor = request.DefaultDepositMinor,
            DefaultCurrency = request.DefaultCurrency,
            Note = request.Note
        };

        db.Units.Add(unit);
        await db.SaveChangesAsync(cancellationToken);

        return unit.PublicId;
    }
}
