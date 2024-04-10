using System.Runtime.Serialization;

namespace WebApiApplication.Exceptions;

public class GameException : Exception
{
    public int ErrorCode { get; set; }

    public GameException(int errorCode, string? message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public GameException(string? message) :
        base(message)
    {
    }

    public GameException(int errorCode, string? message, Exception? innerException) :
        base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public GameException(string? message, Exception? innerException) :
        base(message, innerException)
    {
    }

    protected GameException(SerializationInfo info, StreamingContext context) :
        base(info, context)
    {
    }
}
