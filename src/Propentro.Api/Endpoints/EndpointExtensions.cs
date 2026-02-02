namespace Propentro.Api.Endpoints;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPropertyEndpoints();
        app.MapUnitEndpoints();
        app.MapLeaseEndpoints();
        app.MapPartyEndpoints();

        return app;
    }
}
