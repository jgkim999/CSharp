namespace Demo.Application.Exceptions;

public class UserAlreadyExistException : BusinessException
{
    public UserAlreadyExistException() : base()
    {
    }

    public UserAlreadyExistException(string message) : base(message)
    {
    }

    public UserAlreadyExistException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
