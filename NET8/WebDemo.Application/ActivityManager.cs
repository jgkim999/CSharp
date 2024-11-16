using System.Diagnostics;
using Microsoft.Extensions.Logging;

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
}
