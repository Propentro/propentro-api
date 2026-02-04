using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Leases.Commands.DeleteLease;

public sealed class DeleteLeaseHandler(
    IAppDbContext db) : ICommandHandler<DeleteLeaseCommand>
{
    public async Task<Result> Handle(DeleteLeaseCommand request, CancellationToken cancellationToken)
    {
        var lease = await db.Leases
            .FirstOrDefaultAsync(l => l.PublicId == request.PublicId, cancellationToken);

        if (lease is null)
        {
            return LeaseErrors.NotFoundById(request.PublicId);
        }

        lease.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
