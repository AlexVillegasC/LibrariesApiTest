using System.Reflection;

namespace LibraryService.WebAPI.Shared.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddFeatureEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
            services.AddSingleton(typeof(IEndpoint), type);

        return services;
    }

    public static WebApplication MapFeatureEndpoints(this WebApplication app)
    {
        foreach (var endpoint in app.Services.GetServices<IEndpoint>())
            endpoint.MapEndpoint(app);

        return app;
    }
}
