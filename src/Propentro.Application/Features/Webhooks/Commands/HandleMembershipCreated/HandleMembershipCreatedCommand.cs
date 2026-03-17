namespace Propentro.Application.Features.Webhooks.Commands.HandleMembershipCreated;

/// <summary>
/// Processes a Clerk organizationMembership.created webhook.
/// Upserts the user and resolves pending invites for this org.
/// </summary>
public sealed record HandleMembershipCreatedCommand(
    string UserExternalId,
    string Email,
    string? FirstName,
    string? LastName,
    string ClerkOrganizationId
) : ICommand;
