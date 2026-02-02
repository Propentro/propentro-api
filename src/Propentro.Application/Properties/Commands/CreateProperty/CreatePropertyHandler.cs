using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;

namespace Propentro.Application.Properties.Commands.CreateProperty;

public sealed class CreatePropertyHandler(
    IAppDbContext db,
    ICurrentUser currentUser) : ICommandHandler<CreatePropertyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePropertyCommand request, CancellationToken ct)
    {
        // Parent property validation
        if (request.ParentPropertyId.HasValue)
        {
            var parentExists = await db.Properties
                .AnyAsync(p => p.Id == request.ParentPropertyId.Value, ct);

            if (!parentExists)
            {
                return PropertyErrors.InvalidParentProperty;
            }
        }

        // Duplicate reference code check
        if (!string.IsNullOrEmpty(request.ReferenceCode))
        {
            var refExists = await db.Properties
                .AnyAsync(p => p.ReferenceCode == request.ReferenceCode, ct);

            if (refExists)
            {
                return PropertyErrors.ReferenceCodeExists;
            }
        }

        var property = new Property
        {
            Name = request.Name,
            PropertyKind = request.PropertyKind,
            AddressId = request.AddressId,
            ParentPropertyId = request.ParentPropertyId,
            YearBuilt = request.YearBuilt,
            TotalAreaSqm = request.TotalAreaSqm,
            TotalFloors = request.TotalFloors,
            ReferenceCode = request.ReferenceCode,
            Note = request.Note
        };

        db.Properties.Add(property);
        await db.SaveChangesAsync(ct);

        return property.Id;
    }
}
