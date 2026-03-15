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
    private readonly string? _secretKey = configuration["Clerk:SecretKey"];

    public async Task<Result> UpdateOrganizationAsync(
        string externalId,
        string name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

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

            return Result.Failure(new Error("Clerk.UpdateFailed", body, ErrorType.Failure));
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error syncing organization {ExternalId} to Clerk", externalId);
            return Result.Failure(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));
        }
    }

    public async Task<Result> InviteMemberAsync(
        string orgExternalId,
        string email,
        string clerkRole,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

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
                return Result.Failure(new Error("Member.AlreadyInvited",
                    "This user has already been invited or is already a member.", ErrorType.Conflict));

            Log.Warning("Clerk invitation failed {StatusCode} for {Email}. Body: {Body}",
                response.StatusCode, email, body);

            return Result.Failure(new Error("Clerk.InvitationFailed", body, ErrorType.Failure));
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error inviting {Email} to organization {OrgExternalId}", email, orgExternalId);
            return Result.Failure(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));
        }
    }

    public async Task<Result> AddMemberAsync(
        string orgExternalId,
        string userExternalId,
        string clerkRole,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

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

            return Result.Failure(new Error("Clerk.AddMemberFailed", body, ErrorType.Failure));
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error adding {UserExternalId} to organization {OrgExternalId}", userExternalId, orgExternalId);
            return Result.Failure(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));
        }
    }

    public async Task<Result> RemoveMemberAsync(
        string orgExternalId,
        string userExternalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

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

            return Result.Failure(new Error("Clerk.RemoveMemberFailed", body, ErrorType.Failure));
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error removing {UserExternalId} from organization {OrgExternalId}", userExternalId, orgExternalId);
            return Result.Failure(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));
        }
    }

    public async Task<Result<string>> CreateOrganizationAsync(
        string name, string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure<string>(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

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

            return Result.Failure<string>(new Error("Clerk.CreateOrgFailed", errorBody, ErrorType.Failure));
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error creating organization in Clerk");
            return Result.Failure<string>(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));
        }
    }

    public async Task<Result> DeleteOrganizationAsync(
        string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

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

            return Result.Failure(new Error("Clerk.DeleteOrgFailed", body, ErrorType.Failure));
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error deleting organization {ExternalId} from Clerk", externalId);
            return Result.Failure(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));
        }
    }

    public async Task<Result> RevokeInvitationAsync(
        string externalOrgId, string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            return Result.Failure(new Error("Clerk.NotConfigured", "Clerk secret key is not configured. Cannot sync to Clerk.", ErrorType.Failure));

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
}
