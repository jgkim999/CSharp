namespace WebApiDomain.Models;

public class ResBase
{
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public ResBase()
    {
    }

    public ResBase(int errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
