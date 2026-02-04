
namespace Propentro.Application.Leases.Commands.UpdateLease;

public sealed record UpdateLeaseCommand(
    string PublicId,
    string? Status,
    DateOnly? EndDate,
    DateOnly? MoveOutDate,
    long? RentMinor,
    string? RentFrequency,
    int? RentDueDay,
    string? DepositStatus,
    int? NoticePeriodDays,
    string? Note
) : ICommand;
