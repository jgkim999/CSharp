using System.Net;

namespace DemoApplication.Models;

public class ExceptionResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public string Msg { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;

    public ExceptionResponse()
    {
    }

    public ExceptionResponse(HttpStatusCode statusCode, string msg)
    {
        StatusCode = statusCode;
        Msg = msg;
    }
}