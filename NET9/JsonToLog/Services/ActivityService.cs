using Serilog;

using System.Diagnostics;

namespace JsonToLog.Services;

public static class ActivityService
{
    private static ActivitySource? _activeSource;
    
    public static void Initialize(string name, string version = "1.0.0")
    {
        if (_activeSource != null)
        {
            return; // Already initialized
        }
        _activeSource = new ActivitySource(name, version);
    }
    
    public static string Name => _activeSource?.Name ?? "AppActivitySource";
    
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        if (_activeSource == null)
        {
            Log.Logger.Error("ActiveSourceService is not initialized. Call Initialize() first. {Name}", name);
            return null;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            Log.Logger.Error("Activity name cannot be null or empty. {Name}", name);
            return null;
        }
        if (kind == ActivityKind.Internal && !_activeSource.HasListeners())
        {
            Log.Logger.Warning("No listeners for internal activity. Activity will not be started. {Name}", name);
            return null; // No listeners for internal activities
        }
        // Start a new activity with the provided name and kind
        var activity = _activeSource.StartActivity(name, kind);
        return activity;
    }
}
