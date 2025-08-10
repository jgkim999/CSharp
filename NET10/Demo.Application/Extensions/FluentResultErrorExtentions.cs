using FluentResults;

namespace Demo.Application.Extensions;

public static class FluentResultErrorExtentions
{
    public static string GetErrorMessageAll(this Result result)
    {
        if (result.IsSuccess)
            return string.Empty;
        var errorMessage = string.Join(", ", result.Errors.Select(x => x.Message));
        return errorMessage;
    }
}
