using System.Net;

using WebApiDemo.Models;

namespace WebApiDemo.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (AccessViolationException avEx)
        {
            _logger.LogError($"A new violation exception has been thrown: {avEx}");
            await HandleExceptionAsync(httpContext, avEx);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        string message = exception switch
        {
            AccessViolationException => "Access violation error from the custom middleware",
            _ => "Internal Server Error from the custom middleware."
        };

        await context.Response.WriteAsync(new ErrorDetails()
        {
            StatusCode = context.Response.StatusCode,
            //Message = exception.Message
            Message = message
        }.ToString());
    }
}
