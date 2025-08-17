using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Demo.Application.Extensions;

/// <summary>
/// FastEndpoints 설정을 위한 확장 메서드
/// </summary>
public static class FastEndpointsExtensions
{
    /// <summary>
    /// 기본 예외 처리, API 버전 관리 및 표준화된 오류 응답과 함께 FastEndpoints를 사용하도록 WebApplication을 구성합니다
    /// </summary>
    /// <param name="app">구성할 WebApplication 인스턴스</param>
    /// <returns>구성된 WebApplication 인스턴스</returns>
    public static WebApplication UseFastEndpointsInitialize(this WebApplication app)
    {
        app.UseDefaultExceptionHandler();
        app.UseFastEndpoints(c =>
        {
            c.Versioning.Prefix = "v";
            c.Errors.UseProblemDetails();
            /*
            c.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
            {
                return new ValidationProblemDetails(failures.GroupBy(f => f.PropertyName)
                    .ToDictionary(keySelector: e => e.Key,
                        elementSelector: e => e.Select(m => m.ErrorMessage).ToArray()))
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = statusCode,
                    Instance = ctx.Request.Path,
                    Extensions = { { "traceId", ctx.TraceIdentifier } }
                };
            };
            */
        });
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle(app.Environment.ApplicationName)
                    .WithTheme(ScalarTheme.None)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.RestSharp)
                    .WithCdnUrl("https://cdn.jsdelivr.net/npm/@scalar/api-reference@latest/dist/browser/standalone.js");
            });
        }
        return app;
    }
}
