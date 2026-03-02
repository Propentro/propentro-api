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

            // 404 means org doesn't exist in Clerk (e.g. dev/test env) — treat as success
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Warning("Organization {ExternalId} not found in Clerk — skipping sync", externalId);
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log.Warning(
                "Clerk organization update returned {StatusCode} for {ExternalId}. Body: {Body}",
                response.StatusCode, externalId, body);

            return Result.Failure(new Error("Clerk.UpdateFailed", body, ErrorType.Failure));
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error syncing organization {ExternalId} to Clerk", externalId);
            return Result.Failure(new Error("Clerk.Unavailable", ex.Message, ErrorType.Failure));
        }
    }
}
