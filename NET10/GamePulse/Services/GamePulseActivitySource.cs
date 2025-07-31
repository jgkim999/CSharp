using System.Diagnostics;

namespace GamePulse.Services;

/// <summary>
/// 
/// </summary>
public static class GamePulseActivitySource
{
    /// <summary>
    /// 
    /// </summary>
    private static ActivitySource? _activitySource;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="version"></param>
    public static void Initialize(string serviceName, string version)
    {
        _activitySource = new ActivitySource(serviceName, version);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="kind"></param>
    /// <returns></returns>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        if (_activitySource is null)
            return null;
        var span = _activitySource.StartActivity(name, kind);
        if (span is null)
            return null;
        return span;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="kind"></param>
    /// <param name="parentActivity"></param>
    /// <returns></returns>
    public static Activity? StartActivity(string name, ActivityKind kind, Activity? parentActivity)
    {
        if (parentActivity is null)
            return null;
        if (_activitySource is null)
            return null;
        var span = _activitySource.StartActivity(name, kind, parentActivity.Context);
        if (span is null)
            return null;
        Debug.Assert(parentActivity.Id != null, "parentActivity.Id != null");
        //span.SetParentId(parentActivity.Id);
        return span;
    }
}
