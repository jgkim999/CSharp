using GamePulse.Sod.Services;

namespace GamePulse.Sod;

public static class SodInitialize
{
    public static IServiceCollection AddSod(this IServiceCollection services)
    {
        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddHostedService<SodBackgroundWorker>();
        
        return services;
    }
}
