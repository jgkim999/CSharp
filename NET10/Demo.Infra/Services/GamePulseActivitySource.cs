using System.Diagnostics;

namespace Demo.Infra.Services;

/// <summary>
/// 텔레메트리 추적을 위한 ActivitySource 관리 클래스
/// </summary>
public static class GamePulseActivitySource
{
    private static ActivitySource? _activitySource;

    /// <summary>
    /// 지정된 서비스 이름과 버전으로 추적을 위한 활동 소스를 초기화합니다
    /// </summary>
    /// <param name="serviceName">활동 소스와 연결할 서비스 이름</param>
    /// <summary>
    /// Initializes the internal ActivitySource used for telemetry tracing for the specified service and version.
    /// </summary>
    /// <param name="serviceName">Name of the service to associate with the ActivitySource.</param>
    /// <param name="version">Service version (e.g., semantic version) to associate with the ActivitySource.</param>
    public static void Initialize(string serviceName, string version)
    {
        _activitySource = new ActivitySource(serviceName, version);
    }

    /// <summary>
    /// 초기화된 활동 소스를 사용하여 지정된 이름과 종류로 새 활동을 시작하고 반환합니다
    /// </summary>
    /// <param name="name">시작할 활동의 이름</param>
    /// <param name="kind">생성할 활동의 종류. 기본값은 ActivityKind.Internal</param>
    /// <summary>
    /// Starts and returns an Activity from the internal ActivitySource.
    /// </summary>
    /// <param name="name">Logical name of the activity/span to start.</param>
    /// <param name="kind">The ActivityKind for the new activity. Defaults to <see cref="ActivityKind.Internal"/>.</param>
    /// <returns>The started <see cref="Activity"/>, or <c>null</c> if the underlying ActivitySource is not initialized or the activity could not be created.</returns>
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
    /// 제공된 부모 활동의 컨텍스트를 사용하여 지정된 이름과 종류로 새 활동을 시작합니다
    /// </summary>
    /// <param name="name">시작할 활동의 이름</param>
    /// <param name="kind">생성할 활동의 종류</param>
    /// <param name="parentActivity">새 활동에 사용될 컨텍스트를 가진 부모 활동</param>
    /// <summary>
    /// Starts a child Activity with the specified name and kind using the provided parent activity's context.
    /// </summary>
    /// <param name="name">The operation name for the new Activity.</param>
    /// <param name="kind">The ActivityKind for the new Activity.</param>
    /// <param name="parentActivity">The parent Activity whose Context is used to create the child. If null, no Activity is started and the method returns null.</param>
    /// <returns>The newly started Activity, or null if the parentActivity is null, the internal ActivitySource is not initialized, or starting the Activity failed.</returns>
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