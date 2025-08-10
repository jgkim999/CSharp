using FluentResults;

namespace Demo.Application.Extensions;

public static class FluentResultErrorExtentions
{
    /// <summary>
    /// Returns a single string containing all error messages from the specified <see cref="Result"/>, separated by commas.
    /// </summary>
    /// <param name="result">The <see cref="Result"/> instance to extract error messages from.</param>
    /// <returns>
    /// A comma-separated string of all error messages if the result indicates failure; otherwise, an empty string if the result is successful.
    /// </returns>
    public static string GetErrorMessageAll(this Result result)
    {
        if (result.IsSuccess)
            return string.Empty;
        var errorMessage = string.Join(", ", result.Errors.Select(x => x.Message).ToList());
        return errorMessage;
    }
}
