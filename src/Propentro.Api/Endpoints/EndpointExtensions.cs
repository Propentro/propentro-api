using Propentro.Api.Endpoints.Admin;
using Propentro.Api.Endpoints.Organizations;

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
        app.MapAdminUserEndpoints();
        app.MapAdminMemberEndpoints();
        app.MapAdminInviteEndpoints();

        // Organization Invites
        app.MapOrganizationInviteEndpoints();

        return app;
    }
}
