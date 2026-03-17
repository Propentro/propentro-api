namespace Propentro.Contracts.Admin.Me;

public sealed record AdminMeResponse
{
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string AdminRole { get; init; } = null!;
    public DateTimeOffset AssignedAt { get; init; }
}
