using Propentro.Api.Endpoints.Admin;

namespace Propentro.Api.Endpoints;

internal static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        // Webhooks
        app.MapWebhookEndpoints();

        // Auth
        app.MapMeEndpoints();

        // Organization
        app.MapOrganizationSettingsEndpoints();
        app.MapOrganizationMemberEndpoints();
        app.MapOrganizationRoleEndpoints();

        // Admin
        app.MapAdminOrganizationEndpoints();

        return app;
    }
}
