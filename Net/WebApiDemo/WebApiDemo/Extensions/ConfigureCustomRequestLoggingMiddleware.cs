using WebApiDemo.Middlewares;

namespace WebApiDemo.Extensions;

public static class ConfigureCustomRequestLoggingMiddleware
{
    public static void UseCustomRequestLoggingMiddleware(this WebApplication app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
