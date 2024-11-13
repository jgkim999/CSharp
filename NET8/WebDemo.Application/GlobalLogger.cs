using System.Runtime.CompilerServices;
using Serilog;

namespace WebDemo.Application;

public static class GlobalLogger
{
    public static Serilog.ILogger ForContext(
        string logName = "default",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string method = "")
    {
        return Log.ForContext("LogName", logName)
            //.ForContext("ClassName", typeof(T).Name)
            .ForContext("Method", method)
            .ForContext("FileName", Path.GetFileName(filePath))
            .ForContext("LineNumber", lineNumber);  
    }
    
    public static Serilog.ILogger GetLogger<T>(
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string method = "")
    {
        return Log.ForContext<T>()
            .ForContext("ClassName", typeof(T).Name)
            .ForContext("Method", method)
            .ForContext("Filename", Path.GetFileName(filePath))
            .ForContext("LineNumber", lineNumber);  
    }
}
// Compare this snippet from WebDemo.Application/Services/WeatherForecastService.cs: