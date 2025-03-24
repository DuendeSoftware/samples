namespace OpenApi.Bff.OpenApi;

public static class OpenApiDocumentCombinerExtensions
{
    public static IServiceCollection AddOpenApiDocumentsCombiner(this IServiceCollection services, Action<OpenApiDocumentCombinerOptions> options)
    {
        services.Configure<OpenApiDocumentCombinerOptions>(options);
        services.AddTransient<OpenApiDocumentCombiner>();
        return services;
    }
}