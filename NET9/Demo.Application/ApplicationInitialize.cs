using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

namespace Demo.Application;

public static class ApplicationInitialize
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance =
                    $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

                Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
            };
        });
        //services.AddExceptionHandler<CustomExceptionHandler>();
        return services;
    }
}
