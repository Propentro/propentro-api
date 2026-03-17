namespace Propentro.Api.Tests.Shared;

/// <summary>
/// Extension methods on <see cref="PropentroApiFactory"/> for creating pre-configured HTTP clients.
/// </summary>
internal static class HttpClientExtensions
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> authenticated as the specified user and organization.
    /// </summary>
    public static HttpClient CreateClientAs(
        this PropentroApiFactory factory,
        string externalUserId,
        string externalOrgId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestConstants.UserIdHeader, externalUserId);
        client.DefaultRequestHeaders.Add(TestConstants.OrgIdHeader, externalOrgId);
        return client;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> authenticated as User 1 in Organization 1.
    /// User 1 has all permissions via the default role.
    /// </summary>
    public static HttpClient CreateClientAsUser1(this PropentroApiFactory factory) =>
        factory.CreateClientAs(factory.Seeder.User1ExternalId, factory.Seeder.Organization1ExternalId);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> authenticated as User 2 in Organization 2.
    /// </summary>
    public static HttpClient CreateClientAsUser2(this PropentroApiFactory factory) =>
        factory.CreateClientAs(factory.Seeder.User2ExternalId, factory.Seeder.Organization2ExternalId);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> authenticated as the Admin user in Organization 1.
    /// The Admin user is also a system administrator with the super_admin role.
    /// </summary>
    public static HttpClient CreateClientAsAdmin(this PropentroApiFactory factory) =>
        factory.CreateClientAs(factory.Seeder.AdminUserExternalId, factory.Seeder.Organization1ExternalId);

    /// <summary>
    /// Creates an unauthenticated <see cref="HttpClient"/> with no auth headers.
    /// </summary>
    public static HttpClient CreateAnonymousClient(this PropentroApiFactory factory) =>
        factory.CreateClient();
}
