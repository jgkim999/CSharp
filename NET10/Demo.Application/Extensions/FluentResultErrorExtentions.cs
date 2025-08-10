using FluentResults;

namespace Demo.Application.Extensions;

public static class FluentResultErrorExtentions
{
    /// <summary>
    /// Returns a single string containing all error messages from a failed <see cref="Result"/>, separated by commas.
    /// </summary>
    /// <param name="result">The <see cref="Result"/> instance to extract error messages from.</param>
    /// <returns>
    /// An empty string if the result indicates success; otherwise, a comma-separated string of all error messages.
    /// </returns>
    public static string GetErrorMessageAll(this Result result)
    {
        if (result.IsSuccess)
            return string.Empty;
        var errorMessage = string.Join(", ", result.Errors.Select(x => x.Message));
        return errorMessage;
    }
}
