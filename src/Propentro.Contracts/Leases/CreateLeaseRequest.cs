namespace Propentro.Contracts.Leases;

public record CreateLeaseRequest(
    string? LeaseType,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? MoveInDate,
    string? ReferenceCode,
    long? RentMinor,
    string? Currency,
    string? RentFrequency,
    int? RentDueDay,
    long? DepositMinor,
    int? NoticePeriodDays,
    string? Note,
    IReadOnlyList<CreateLeaseUnitRequest> Units,
    IReadOnlyList<CreateLeasePartyRequest> Parties);

public record CreateLeaseUnitRequest(
    Guid UnitId,
    bool IsPrimary);

public record CreateLeasePartyRequest(
    Guid PartyId,
    string Role,
    bool IsPrimary,
    bool IsSignatory,
    bool IsOccupant,
    string? Note);
