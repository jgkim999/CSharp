using FastEndpoints.Swagger;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Application.Extensions;

/// <summary>
/// OpenAPI 서비스 설정을 위한 확장 메서드
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// OpenAPI 및 Swagger 문서 서비스를 의존성 주입 컨테이너에 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <summary>
    /// Configures OpenAPI/Swagger documents and registers OpenAPI services on the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// Registers three Swagger documents (initial, release 1, release 2) with distinct document names and versions,
    /// and adds OpenAPI endpoints for "v1" and "v2".
    /// </remarks>
    /// <returns>The same <see cref="IServiceCollection"/> instance after OpenAPI/Swagger configuration.</returns>
    public static IServiceCollection AddOpenApiServices(this IServiceCollection services)
    {
        services.SwaggerDocument(o =>
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

        // OpenAPI 서비스를 컨테이너에 추가
        // ASP.NET Core OpenAPI 구성에 대한 자세한 내용: https://aka.ms/aspnet/openapi
        services.AddOpenApi("v1");
        services.AddOpenApi("v2");

        return services;
    }
}