namespace Propentro.Contracts.Leases;

public record LeaseDetailDto(
    string PublicId,
    string LeaseType,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? MoveInDate,
    DateOnly? MoveOutDate,
    string? ReferenceCode,
    long? RentMinor,
    string Currency,
    string RentFrequency,
    int? RentDueDay,
    long? DepositMinor,
    string? DepositStatus,
    string? DepositHeldBy,
    DateOnly? DepositReceivedAt,
    DateOnly? DepositReturnedAt,
    long? DepositReturnMinor,
    int? NoticePeriodDays,
    string? Note,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? PreviousLeasePublicId,
    IReadOnlyList<LeaseUnitDto> Units,
    IReadOnlyList<LeasePartyDto> Parties);

public record LeaseUnitDto(
    string UnitPublicId,
    string UnitName,
    string PropertyName,
    bool IsPrimary);

public record LeasePartyDto(
    string PartyPublicId,
    string PartyDisplayName,
    string Role,
    bool IsPrimary,
    bool IsSignatory,
    bool IsOccupant);
