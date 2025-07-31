using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace GamePulse;

/// <summary>
/// 
/// </summary>
public static class FastEndpointsInitialize
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
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
        return app;
    }
}