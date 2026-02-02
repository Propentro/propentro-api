using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Properties.Commands.UpdateProperty;

public sealed class UpdatePropertyHandler(
    IAppDbContext db,
    ICurrentUser currentUser) : ICommandHandler<UpdatePropertyCommand>
{
    public async Task<Result> Handle(UpdatePropertyCommand request, CancellationToken ct)
    {
        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, ct);

        if (property is null)
        {
            return PropertyErrors.NotFoundById(request.PublicId);
        }

        if (request.Name is not null) property.Name = request.Name;
        if (request.PropertyKind is not null) property.PropertyKind = request.PropertyKind;
        if (request.AddressId.HasValue) property.AddressId = request.AddressId;
        if (request.YearBuilt.HasValue) property.YearBuilt = request.YearBuilt;
        if (request.TotalAreaSqm.HasValue) property.TotalAreaSqm = request.TotalAreaSqm;
        if (request.TotalFloors.HasValue) property.TotalFloors = request.TotalFloors;
        if (request.ReferenceCode is not null) property.ReferenceCode = request.ReferenceCode;
        if (request.Note is not null) property.Note = request.Note;

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
