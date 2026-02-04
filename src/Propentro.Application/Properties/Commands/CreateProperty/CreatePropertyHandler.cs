using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;

namespace Propentro.Application.Properties.Commands.CreateProperty;

public sealed class CreatePropertyHandler(
    IAppDbContext db) : ICommandHandler<CreatePropertyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        // Parent property validation
        if (request.ParentPropertyId.HasValue)
        {
            var parentExists = await db.Properties
                .AnyAsync(p => p.Id == request.ParentPropertyId.Value, cancellationToken);

            if (!parentExists)
            {
                return PropertyErrors.InvalidParentProperty;
            }
        }

        // Duplicate reference code check
        if (!string.IsNullOrEmpty(request.ReferenceCode))
        {
            var refExists = await db.Properties
                .AnyAsync(p => p.ReferenceCode == request.ReferenceCode, cancellationToken);

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
        await db.SaveChangesAsync(cancellationToken);

        return property.Id;
    }
}
