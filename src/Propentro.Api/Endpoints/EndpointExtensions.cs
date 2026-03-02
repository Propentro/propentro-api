namespace Propentro.Api.Endpoints;

internal static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMeEndpoints();
        app.MapOrganizationEndpoints();
        app.MapWebhookEndpoints();
        app.MapContactEndpoints();

        return app;
    }
}
