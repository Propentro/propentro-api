using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Properties.Commands.DeleteProperty;

public sealed class DeletePropertyHandler(
    IAppDbContext db) : ICommandHandler<DeletePropertyCommand>
{
    public async Task<Result> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.PublicId == request.PublicId, cancellationToken);

        if (property is null)
        {
            return PropertyErrors.NotFoundById(request.PublicId);
        }

        // Soft delete
        property.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
