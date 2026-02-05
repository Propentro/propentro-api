using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Leases.Commands.UpdateLease;

public sealed class UpdateLeaseHandler(
    IAppDbContext db) : ICommandHandler<UpdateLeaseCommand>
{
    public async Task<Result> Handle(UpdateLeaseCommand request, CancellationToken cancellationToken)
    {
        var lease = await db.Leases
            .FirstOrDefaultAsync(l => l.PublicId == request.PublicId, cancellationToken);

        if (lease is null)
        {
            return LeaseErrors.NotFoundById(request.PublicId);
        }

        if (request.Status is not null) { lease.Status = request.Status; }
        if (request.EndDate.HasValue) { lease.EndDate = request.EndDate; }
        if (request.MoveOutDate.HasValue) { lease.MoveOutDate = request.MoveOutDate; }
        if (request.RentMinor.HasValue) { lease.RentMinor = request.RentMinor; }
        if (request.RentFrequency is not null) { lease.RentFrequency = request.RentFrequency; }
        if (request.RentDueDay.HasValue) { lease.RentDueDay = request.RentDueDay; }
        if (request.DepositStatus is not null) { lease.DepositStatus = request.DepositStatus; }
        if (request.NoticePeriodDays.HasValue) { lease.NoticePeriodDays = request.NoticePeriodDays; }
        if (request.Note is not null) { lease.Note = request.Note; }

        return Result.Success();
    }
}
