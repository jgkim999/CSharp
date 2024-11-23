using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serilog;

namespace WebDemo.Application;

public static class GlobalLogger
{
    public static Serilog.ILogger ForContext(
        string contextName = "default",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string method = "")
    {
        var logger = Log.ForContext("ContextName", contextName)
            .ForContext("Method", method)
            .ForContext("Filename", Path.GetFileName(filePath))
            .ForContext("LineNumber", lineNumber);
        return logger;
    }

    public static Serilog.ILogger ForContext(
        Activity? activity,
        string contextName = "default",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string method = "")
    {
        var logger = Log.ForContext("ContextName", contextName)
            .ForContext("Method", method)
            .ForContext("Filename", Path.GetFileName(filePath))
            .ForContext("LineNumber", lineNumber);
        if (activity is not null)
            logger.ForContext("TraceId", activity.TraceId.ToHexString());
        return logger;
    }

    public static Serilog.ILogger GetLogger<T>(
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string method = "")
    {
        var logger = Log.ForContext<T>()
            .ForContext("ClassName", typeof(T).Name)
            .ForContext("Method", method)
            .ForContext("Filename", Path.GetFileName(filePath))
            .ForContext("LineNumber", lineNumber);
        return logger;
    }

    public static Serilog.ILogger GetLogger<T>(
        Activity? activity,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string method = "")
    {
        var logger = Log.ForContext<T>()
            .ForContext("ClassName", typeof(T).Name)
            .ForContext("Method", method)
            .ForContext("Filename", Path.GetFileName(filePath))
            .ForContext("LineNumber", lineNumber);
        if (activity is not null)
            logger.ForContext("TraceId", activity.TraceId.ToHexString());
        return logger;
    }
}
// Compare this snippet from WebDemo.Application/Services/WeatherForecastService.cs: