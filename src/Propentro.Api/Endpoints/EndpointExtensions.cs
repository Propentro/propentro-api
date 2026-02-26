namespace Propentro.Api.Endpoints;

internal static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMeEndpoints();
        app.MapWebhookEndpoints();
        app.MapContactEndpoints();
        app.MapLookupEndpoints();

        return app;
    }
}
