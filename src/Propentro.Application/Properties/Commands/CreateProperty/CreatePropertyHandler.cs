using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;

namespace Propentro.Application.Properties.Commands.CreateProperty;

/// <summary>
/// Handler for creating a new property.
/// Returns the PublicId of the created property for use in API responses.
/// </summary>
public sealed class CreatePropertyHandler(
    IAppDbContext db) : ICommandHandler<CreatePropertyCommand, string>
{
    public async Task<Result<string>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        // Parent property validation - ensure it exists and belongs to same organization
        if (request.ParentPropertyId.HasValue)
        {
            var parentExists = await db.Properties
                .AnyAsync(p => p.Id == request.ParentPropertyId.Value, cancellationToken);

            if (!parentExists)
            {
                return PropertyErrors.InvalidParentProperty;
            }
        }

        // Duplicate reference code check within organization
        if (!string.IsNullOrEmpty(request.ReferenceCode))
        {
            var refExists = await db.Properties
                .AnyAsync(p => p.ReferenceCode == request.ReferenceCode, cancellationToken);

            if (refExists)
            {
                return PropertyErrors.ReferenceCodeExists;
            }
        }

        // Create the property entity - Id and PublicId are auto-generated in constructor
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

        // Return PublicId for API consumers (not internal Guid)
        return property.PublicId;
    }
}
