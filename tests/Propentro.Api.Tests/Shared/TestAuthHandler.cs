using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Propentro.Api.Tests.Shared;

/// <summary>
/// Fake authentication handler that reads test identity from custom request headers.
/// Replaces JWT/Clerk auth in integration tests. The <see cref="Propentro.Api.Middleware.UserContextMiddleware"/>
/// runs normally after this handler sets the ClaimsPrincipal, resolving external IDs to internal DB IDs.
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no test user header is present, return NoResult (unauthenticated)
        if (!Request.Headers.TryGetValue(TestConstants.UserIdHeader, out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var externalUserId = userIdValues.ToString();
        if (string.IsNullOrWhiteSpace(externalUserId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, externalUserId),
            new("sub", externalUserId)
        };

        // Add organization claim in the same JSON format Clerk uses ("o" claim with {"id": "org_xxx"})
        if (Request.Headers.TryGetValue(TestConstants.OrgIdHeader, out var orgIdValues))
        {
            var externalOrgId = orgIdValues.ToString();
            if (!string.IsNullOrWhiteSpace(externalOrgId))
            {
                var orgClaimValue = JsonSerializer.Serialize(new { id = externalOrgId });
                claims.Add(new Claim("o", orgClaimValue));
            }
        }

        var identity = new ClaimsIdentity(claims, TestConstants.TestScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestConstants.TestScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
