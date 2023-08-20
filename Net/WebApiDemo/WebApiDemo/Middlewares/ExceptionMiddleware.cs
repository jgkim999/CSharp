using Newtonsoft.Json;
using System.Net;
using WebApiApplication.Exceptions;
using WebApiDemo.Models;
using WebApiDomain.Models;

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
        catch (GameException ex)
        {
            await HandleGameExceptionAsync(httpContext, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: {ex.Message}");
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleGameExceptionAsync(HttpContext context, GameException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        await context.Response.WriteAsync(
            JsonConvert.SerializeObject(
                new ResBase(exception.ErrorCode, exception.Message)));
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        string message = exception switch
        {
            AccessViolationException => "Access violation error from the custom middleware",
            GameException => "Game Exception",
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
