using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Properties.Commands.DeleteProperty;

public sealed class DeletePropertyHandler(
    IAppDbContext db,
    ICurrentUser currentUser) : ICommandHandler<DeletePropertyCommand>
{
    public async Task<Result> Handle(DeletePropertyCommand request, CancellationToken ct)
    {
        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, ct);

        if (property is null)
        {
            return PropertyErrors.NotFoundById(request.PublicId);
        }

        // Soft delete
        property.IsActive = false;
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
