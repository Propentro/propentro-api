using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        {
            Log.Warning("Clerk:SecretKey is not configured — skipping organization sync for {ExternalId}", externalId);
            return Result.Success();
        }

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
        {
            Log.Warning("Clerk:SecretKey is not configured — skipping invitation for {Email}", email);
            return Result.Success();
        }

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
        {
            Log.Warning("Clerk:SecretKey is not configured — skipping member add for {UserExternalId}", userExternalId);
            return Result.Success();
        }

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
        {
            Log.Warning("Clerk:SecretKey is not configured — skipping membership removal for {UserExternalId}", userExternalId);
            return Result.Success();
        }

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
}
