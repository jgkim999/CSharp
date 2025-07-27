using FastEndpoints.Swagger;

namespace GamePulse;

/// <summary>
/// 
/// </summary>
public static class OpenApiInitialize
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    /// <returns></returns>
    public static IServiceCollection AddOpenApiServices(this IServiceCollection service)
    {
        service.SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.SchemaSettings.SchemaNameGenerator = new NJsonSchema.Generation.DefaultSchemaNameGenerator();
                    s.DocumentName = "Initial version";
                    s.Title = "My API";
                    s.Version = "v0";
                };
            })
            .SwaggerDocument(o =>
            {
                o.MaxEndpointVersion = 1;
                o.DocumentSettings = s =>
                {
                    s.SchemaSettings.SchemaNameGenerator = new NJsonSchema.Generation.DefaultSchemaNameGenerator();
                    s.DocumentName = "Release 1";
                    s.Title = "My API";
                    s.Version = "v1";
                };
            })
            .SwaggerDocument(o =>
            {
                o.MaxEndpointVersion = 2;
                o.DocumentSettings = s =>
                {
                    s.SchemaSettings.SchemaNameGenerator = new NJsonSchema.Generation.DefaultSchemaNameGenerator();
                    s.DocumentName = "Release 2";
                    s.Title = "My API";
                    s.Version = "v2";
                };
            });
        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        service.AddOpenApi("v1");
        service.AddOpenApi("v2");

        return service;
    }
}