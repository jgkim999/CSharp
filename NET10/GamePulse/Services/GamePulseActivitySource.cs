using System.Diagnostics;

namespace GamePulse.Services;

public static class GamePulseActivitySource
{
    private static ActivitySource? _activitySource;

    /// <summary>
    /// Initializes the activity source for tracing with the specified service name and version.
    /// </summary>
    /// <param name="serviceName">The name of the service to associate with the activity source.</param>
    /// <param name="version">The version of the service.</param>
    public static void Initialize(string serviceName, string version)
    {
        _activitySource = new ActivitySource(serviceName, version);
    }

    /// <summary>
    /// Starts and returns a new activity with the specified name and kind using the initialized activity source.
    /// </summary>
    /// <param name="name">The name of the activity to start.</param>
    /// <param name="kind">The kind of activity to create. Defaults to <see cref="ActivityKind.Internal"/>.</param>
    /// <returns>The started <see cref="Activity"/> instance, or <c>null</c> if the activity source is not initialized or the activity cannot be started.</returns>
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
    /// Starts a new activity with the specified name and kind, using the provided parent activity's context.
    /// </summary>
    /// <param name="name">The name of the activity to start.</param>
    /// <param name="kind">The kind of activity to create.</param>
    /// <param name="parentActivity">The parent activity whose context will be used for the new activity.</param>
    /// <returns>The newly started <see cref="Activity"/> if successful; otherwise, <c>null</c> if the parent activity or activity source is not initialized, or if the activity could not be started.</returns>
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
