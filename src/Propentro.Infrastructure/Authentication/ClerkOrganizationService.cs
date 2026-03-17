using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Infrastructure.Authentication;

public sealed class ClerkOrganizationService(HttpClient httpClient, IConfiguration configuration)
    : IClerkOrganizationService
{
    private static readonly Error NotConfiguredError = Error.ExternalService(
        "ExternalService.NotConfigured",
        "An external service is not properly configured. Please contact support.");

    private static readonly Error UnavailableError = Error.ExternalService(
        "ExternalService.Unavailable",
        "An external service is temporarily unavailable. Please try again later.");

    private static readonly Error SyncFailedError = Error.ExternalService(
        "ExternalService.SyncFailed",
        "An external service is temporarily unavailable. Please try again later.");

    private readonly string? _secretKey = configuration["Clerk:SecretKey"];

    public async Task<Result> UpdateOrganizationAsync(
        string externalId,
        string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            var response = await httpClient.PatchAsJsonAsync(
                $"organizations/{externalId}",
                new { name },
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return Result.Success();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Warning("Organization {ExternalId} not found in Clerk — skipping sync", externalId);
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Warning("Clerk organization update returned {StatusCode} for {ExternalId}. Body: {Body}",
                response.StatusCode, externalId, body);

            return Result.Failure(SyncFailedError);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error syncing organization {ExternalId} to Clerk", externalId);
            return Result.Failure(UnavailableError);
        }
    }

    public async Task<Result> InviteMemberAsync(
        string orgExternalId,
        string email,
        string clerkRole,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"organizations/{orgExternalId}/invitations",
                new { email_address = email, role = clerkRole },
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return Result.Success();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                return Result.Failure(Error.Conflict(
                    "Invite.AlreadyPending", "A pending invite already exists for this email and organization."));

            Log.Warning("Clerk invitation failed {StatusCode} for {Email}. Body: {Body}",
                response.StatusCode, email, body);

            return Result.Failure(SyncFailedError);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error inviting {Email} to organization {OrgExternalId}", email, orgExternalId);
            return Result.Failure(UnavailableError);
        }
    }

    public async Task<Result> AddMemberAsync(
        string orgExternalId,
        string userExternalId,
        string clerkRole,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"organizations/{orgExternalId}/memberships",
                new { user_id = userExternalId, role = clerkRole },
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return Result.Success();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Warning("User {UserExternalId} not found in Clerk — skipping direct add", userExternalId);
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Warning("Clerk member add failed {StatusCode} for {UserExternalId}. Body: {Body}",
                response.StatusCode, userExternalId, body);

            return Result.Failure(SyncFailedError);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error adding {UserExternalId} to organization {OrgExternalId}", userExternalId, orgExternalId);
            return Result.Failure(UnavailableError);
        }
    }

    public async Task<Result> RemoveMemberAsync(
        string orgExternalId,
        string userExternalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            var response = await httpClient.DeleteAsync(
                $"organizations/{orgExternalId}/memberships/{userExternalId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return Result.Success();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Warning("User {UserExternalId} not found in Clerk org {OrgExternalId} — skipping removal", userExternalId, orgExternalId);
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Warning("Clerk membership removal failed {StatusCode} for {UserExternalId}. Body: {Body}",
                response.StatusCode, userExternalId, body);

            return Result.Failure(SyncFailedError);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error removing {UserExternalId} from organization {OrgExternalId}", userExternalId, orgExternalId);
            return Result.Failure(UnavailableError);
        }
    }

    public async Task<Result<string>> CreateOrganizationAsync(
        string name, string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure<string>(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "organizations",
                new { name, slug },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                var externalId = body.GetProperty("id").GetString()!;
                Log.Information("Created organization in Clerk: {ExternalId}", externalId);
                return externalId;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Warning("Clerk organization create failed {StatusCode}. Body: {Body}",
                response.StatusCode, errorBody);

            return Result.Failure<string>(SyncFailedError);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error creating organization in Clerk");
            return Result.Failure<string>(UnavailableError);
        }
    }

    public async Task<Result> DeleteOrganizationAsync(
        string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            var response = await httpClient.DeleteAsync(
                $"organizations/{externalId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Deleted organization from Clerk: {ExternalId}", externalId);
                return Result.Success();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Warning("Organization {ExternalId} not found in Clerk — already deleted", externalId);
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Warning("Clerk organization delete failed {StatusCode} for {ExternalId}. Body: {Body}",
                response.StatusCode, externalId, body);

            return Result.Failure(SyncFailedError);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error deleting organization {ExternalId} from Clerk", externalId);
            return Result.Failure(UnavailableError);
        }
    }

    public async Task<Result> RevokeInvitationAsync(
        string externalOrgId, string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            // List pending invitations for this org to find the one matching email
            var listResponse = await httpClient.GetAsync(
                $"organizations/{externalOrgId}/invitations?status=pending",
                cancellationToken);

            if (!listResponse.IsSuccessStatusCode)
                return Result.Success(); // Best effort — invitation may not exist

            var invitations = await listResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var matchingInvitation = invitations.GetProperty("data").EnumerateArray()
                .FirstOrDefault(i => string.Equals(
                    i.GetProperty("email_address").GetString(), email, StringComparison.OrdinalIgnoreCase));

            if (matchingInvitation.ValueKind == JsonValueKind.Undefined)
                return Result.Success(); // No matching invitation found

            var invitationId = matchingInvitation.GetProperty("id").GetString()!;

            // Revoke the invitation
            var revokeResponse = await httpClient.PostAsync(
                $"organizations/{externalOrgId}/invitations/{invitationId}/revoke",
                null,
                cancellationToken);

            if (revokeResponse.IsSuccessStatusCode)
            {
                Log.Information("Revoked Clerk invitation {InvitationId} for {Email}", invitationId, email);
            }
            else
            {
                Log.Warning("Failed to revoke Clerk invitation {InvitationId} for {Email} — {StatusCode}",
                    invitationId, email, revokeResponse.StatusCode);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error revoking invitation for {Email} in org {OrgExternalId}", email, externalOrgId);
            return Result.Success(); // Best effort — don't fail the operation
        }
    }

    public async Task<Result> DeleteUserAsync(
        string userExternalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(NotConfiguredError);

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _secretKey);

        try
        {
            var response = await httpClient.DeleteAsync(
                $"users/{userExternalId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("Deleted user from Clerk: {ExternalId}", userExternalId);
                return Result.Success();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.Warning("User {ExternalId} not found in Clerk — already deleted", userExternalId);
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Warning("Clerk user delete failed {StatusCode} for {ExternalId}. Body: {Body}",
                response.StatusCode, userExternalId, body);

            return Result.Failure(SyncFailedError);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error deleting user {ExternalId} from Clerk", userExternalId);
            return Result.Failure(UnavailableError);
        }
    }
}
