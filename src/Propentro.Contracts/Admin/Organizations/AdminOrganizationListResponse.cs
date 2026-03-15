namespace Propentro.Contracts.Admin.Organizations;

public sealed record AdminOrganizationListResponse
{
    public string OrganizationId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string SubscriptionPlan { get; init; } = null!;
    public bool IsActive { get; init; }
    public int MemberCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
