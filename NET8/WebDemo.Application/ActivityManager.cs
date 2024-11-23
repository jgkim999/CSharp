using System.Diagnostics;

namespace WebDemo.Application;

public class ActivityManager
{
    private readonly ActivitySource _activitySource;
    
    public ActivityManager(string name, string version)
    {
        GlobalLogger.GetLogger<ActivityManager>().Information("ActivityManager is created. {name}, {version}", name, version);
        //GlobalLogger.ForContext().Information("ActivityManager is created.");
        
        _activitySource = new ActivitySource(
            name,
            version);
    }
    
    public Activity? StartActivity(string name)
    {
        GlobalLogger.GetLogger<ActivityManager>().Verbose("ActivityManager.StartActivity is called. {name}", name);
        
        Activity? activity = _activitySource.StartActivity(name);
        return activity;
    }
    
    public Activity? StartActivity(string name, ActivityKind kind)
    {
        GlobalLogger.GetLogger<ActivityManager>().Verbose("ActivityManager.StartActivity is called. {name}, {kind}", name, kind);

        Activity? activity = _activitySource.StartActivity(name, kind);
        return activity;
    }

    public Activity? StartActivity(string name, string parentId)
    {
        GlobalLogger.GetLogger<ActivityManager>().Verbose("ActivityManager.StartActivity is called. {name}, {parentId}", name, parentId);

        Activity? activity = _activitySource.StartActivity(name);
        activity?.SetParentId(parentId);
        return activity;
    }

    public Activity? StartActivity(string name, string parentId, ActivityKind kind)
    {
        GlobalLogger.GetLogger<ActivityManager>().Verbose("ActivityManager.StartActivity is called. {name}, {kind} {parentId}", name, kind, parentId);

        Activity? activity = _activitySource.StartActivity(name, kind, parentId);
        return activity;
    }
}
