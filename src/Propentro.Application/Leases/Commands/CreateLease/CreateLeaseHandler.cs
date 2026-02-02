using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;

namespace Propentro.Application.Leases.Commands.CreateLease;

public sealed class CreateLeaseHandler(
    IAppDbContext db) : ICommandHandler<CreateLeaseCommand, string>
{
    public async Task<Result<string>> Handle(CreateLeaseCommand request, CancellationToken ct)
    {
        if (request.EndDate.HasValue && request.EndDate < request.StartDate)
        {
            return LeaseErrors.InvalidDateRange;
        }

        var lease = new Lease
        {
            LeaseType = request.LeaseType ?? "fixed_term",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RentMinor = request.RentMinor,
            Currency = request.Currency ?? "EUR",
            RentFrequency = request.RentFrequency ?? "monthly",
            RentDueDay = request.RentDueDay,
            DepositMinor = request.DepositMinor,
            NoticePeriodDays = request.NoticePeriodDays,
            ReferenceCode = request.ReferenceCode,
            Note = request.Note
        };

        db.Leases.Add(lease);
        await db.SaveChangesAsync(ct);

        return lease.PublicId;
    }
}
