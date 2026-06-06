namespace Demo.Application.Exceptions;

public class UserNotFoundException : BusinessException
{
    public UserNotFoundException()
        : base()
    {
    }

    public UserNotFoundException(string message) 
        : base(message)
    {
    }

    public UserNotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
