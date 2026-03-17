namespace Propentro.Api.Tests.Shared;

/// <summary>
/// Constants used across integration tests for authentication and test identity.
/// </summary>
internal static class TestConstants
{
    /// <summary>
    /// Header name for setting the test user's external ID (maps to ClaimTypes.NameIdentifier).
    /// </summary>
    public const string UserIdHeader = "X-Test-User-Id";

    /// <summary>
    /// Header name for setting the test user's external organization ID (maps to "o" claim).
    /// </summary>
    public const string OrgIdHeader = "X-Test-Org-Id";

    /// <summary>
    /// Default external user ID used in tests (Clerk-style format).
    /// </summary>
    public const string DefaultExternalUserId = "user_test_default";

    /// <summary>
    /// Default external organization ID used in tests (Clerk-style format).
    /// </summary>
    public const string DefaultExternalOrgId = "org_test_default";

    /// <summary>
    /// Authentication scheme name used by the test auth handler.
    /// </summary>
    public const string TestScheme = "TestScheme";
}
