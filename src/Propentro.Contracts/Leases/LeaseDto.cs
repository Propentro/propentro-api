namespace Propentro.Contracts.Leases;

public record LeaseDto(
    string PublicId,
    string LeaseType,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? ReferenceCode,
    long? RentMinor,
    string Currency,
    string RentFrequency,
    DateTime CreatedAt,
    string? PrimaryUnitPublicId,
    string? PrimaryUnitName,
    string? PrimaryTenantName);
