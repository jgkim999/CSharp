using WebApiDemo.Middlewares;

namespace WebApiDemo.Extensions;

public static class CustomExceptionMiddlewareExtention
{
    public static void UseCustomExceptionMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
    }
}
