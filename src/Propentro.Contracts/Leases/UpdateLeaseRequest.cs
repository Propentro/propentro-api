namespace Propentro.Contracts.Leases;

public record UpdateLeaseRequest(
    string? LeaseType,
    string? Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? MoveInDate,
    DateOnly? MoveOutDate,
    string? ReferenceCode,
    long? RentMinor,
    string? Currency,
    string? RentFrequency,
    int? RentDueDay,
    long? DepositMinor,
    string? DepositStatus,
    string? DepositHeldBy,
    DateOnly? DepositReceivedAt,
    DateOnly? DepositReturnedAt,
    long? DepositReturnMinor,
    int? NoticePeriodDays,
    string? Note);
