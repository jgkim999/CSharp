using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace GamePulse;

/// <summary>
///
/// </summary>
public static class FastEndpointsInitialize
{
    /// <summary>
    /// Configures the WebApplication to use FastEndpoints middleware with default exception handling, API versioning, and Problem Details error responses.
    /// </summary>
    /// <param name="app">The WebApplication instance to configure.</param>
    /// <summary>
    /// Configures the WebApplication to use FastEndpoints with default exception handling, API versioning, and standardized error responses.
    /// </summary>
    /// <param name="app">The WebApplication instance to configure.</param>
    /// <returns>The configured WebApplication instance.</returns>
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
