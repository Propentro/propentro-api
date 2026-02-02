namespace Propentro.Contracts.Parties;

public record PartyDto(
    string PublicId,
    string Kind,
    string DisplayName,
    string? Email,
    string? Phone,
    DateTime CreatedAt);
