using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Application.Common.Interfaces.Authentication;
using Serilog;

namespace Propentro.Infrastructure.Authorization;

public sealed class SystemAdminAuthorizationHandler(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache) : IAuthorizationHandler
{
    private const string CacheKeyPrefix = "system_admin:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.HasFailed)
            return;

        var httpContext = context.Resource as HttpContext;
        var endpoint = httpContext?.GetEndpoint();

        var requiresAdmin = endpoint?.Metadata
            .GetMetadata<RequireSystemAdministratorAttribute>();

        if (requiresAdmin is null)
            return;

        if (httpContext is null)
            return;

        var currentUser = httpContext.RequestServices.GetRequiredService<IUserContext>();

        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            context.Fail(new AuthorizationFailureReason(this, "Not authenticated"));
            return;
        }

        // Check 1: Clerk private metadata
        var cacheKey = $"{CacheKeyPrefix}{currentUser.UserId}";

        if (!cache.TryGetValue(cacheKey, out bool isClerkAdmin))
        {
            try
            {
                var secretKey = configuration["Clerk:SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(currentUser.ExternalUserId))
                {
                    context.Fail(new AuthorizationFailureReason(this, "Unable to verify admin status"));
                    return;
                }

                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://api.clerk.com/v1/");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", secretKey);

                var response = await client.GetAsync(
                    $"users/{currentUser.ExternalUserId}",
                    httpContext.RequestAborted);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warning("Failed to fetch user from Clerk API for user {UserId}, status {StatusCode}",
                        currentUser.UserId, response.StatusCode);
                    context.Fail(new AuthorizationFailureReason(this, "Unable to verify admin status"));
                    return;
                }

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(httpContext.RequestAborted);

                var systemRole = json.TryGetProperty("private_metadata", out var metadata)
                    && metadata.TryGetProperty("systemRole", out var roleValue)
                    ? roleValue.GetString()
                    : null;

                isClerkAdmin = systemRole == "super_admin";
                cache.Set(cacheKey, isClerkAdmin, CacheDuration);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to verify system admin status via Clerk for user {UserId}", currentUser.UserId);
                context.Fail(new AuthorizationFailureReason(this, "Unable to verify admin status"));
                return;
            }
        }

        if (!isClerkAdmin)
        {
            Log.Debug("System admin check failed: Clerk private metadata lacks super_admin role for user {UserId}", currentUser.UserId);
            context.Fail(new AuthorizationFailureReason(this, "Not a system administrator"));
            return;
        }

        // Check 2: DB record in system_administrators
        var dbContext = httpContext.RequestServices.GetRequiredService<IAppDbContext>();
        var isAdmin = await dbContext.SystemAdministrators
            .IgnoreQueryFilters()
            .AnyAsync(
                sa => sa.UserId == currentUser.UserId && sa.IsActive,
                httpContext.RequestAborted);

        if (!isAdmin)
        {
            Log.Warning("System admin check failed: no active DB record for user {UserId} despite valid Clerk metadata", currentUser.UserId);
            context.Fail(new AuthorizationFailureReason(this, "Not a system administrator"));
            return;
        }

        Log.Debug("System admin access granted for user {UserId}", currentUser.UserId);
    }
}
