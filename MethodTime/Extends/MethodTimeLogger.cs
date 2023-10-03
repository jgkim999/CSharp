using System.Reflection;

namespace MethodTime.Extends;

public static class MethodTimeLogger
{
    public static void Log(MethodBase methodBase, TimeSpan elapsed, string message)
    {
        // Do some logging here
        Serilog.Log.Information(
            "{0}.{1} {2}ms {3}",
            methodBase.DeclaringType!.Name,
            methodBase.Name,
            elapsed.Milliseconds.ToString(),
            message);
    }
}