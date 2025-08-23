using System.Runtime.CompilerServices;
using Serilog;

namespace Demo.Application.Extensions;

public static class LoggerExtensions
{
    /// <summary>
    /// SourceFilePath, MemberName, SourceLineNumber 를 추가한다.
    /// </summary>
    /// <param name="logger">Serilog</param>
    /// <param name="sourceFilePath"></param>
    /// <param name="memberName"></param>
    /// <param name="sourceLineNumber"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ILogger WithClassAndMethodNames<T>(
        this ILogger logger,
        [CallerFilePath] string sourceFilePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var className = typeof(T).Name;
        return logger.ForContext("ClassName", className)
            .ForContext("FilePath", sourceFilePath)
            .ForContext("MethodName", memberName)
            .ForContext("LineNumber", sourceLineNumber);
    }
}
