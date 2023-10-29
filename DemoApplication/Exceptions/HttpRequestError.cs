namespace DemoApplication.Exceptions;

public class HttpRequestError : Exception
{
    public HttpRequestError()
    {
    }

    public HttpRequestError(string? message) 
        : base(message)
    {
    }

    public HttpRequestError(string? message, Exception? innerException) 
        : base(message, innerException)
    {
    }
}