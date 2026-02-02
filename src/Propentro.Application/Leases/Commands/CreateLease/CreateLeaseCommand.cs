using Propentro.Application.Common.Interfaces;

namespace Propentro.Application.Leases.Commands.CreateLease;

public sealed record CreateLeaseCommand(
    string? LeaseType,
    DateOnly StartDate,
    DateOnly? EndDate,
    long? RentMinor,
    string? Currency,
    string? RentFrequency,
    int? RentDueDay,
    long? DepositMinor,
    int? NoticePeriodDays,
    string? ReferenceCode,
    string? Note
) : ICommand<string>;
