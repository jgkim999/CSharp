using GamePulse.Sod.Metrics;
using GamePulse.Sod.Services;

namespace GamePulse.Sod;

public static class SodInitialize
{
    /// <summary>
    /// Registers the SOD background task queue and worker services with the dependency injection container.
    /// </summary>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddSod(this IServiceCollection services)
    {
        services.AddSingleton<SodMetrics>();
        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddHostedService<SodBackgroundWorker>();

        return services;
    }
}
