using GamePulse.Sod.Services;
using GamePulse.Configs;
using Demo.Application.Services;
using Microsoft.Extensions.Options;

namespace GamePulse.Sod;

public static class SodInitialize
{
    /// <summary>
    /// SOD 백그라운드 작업 큐 및 워커 서비스와 텔레메트리 서비스를 의존성 주입 컨테이너에 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>업데이트된 <see cref="IServiceCollection"/> 인스턴스</returns>
    public static IServiceCollection AddSod(this IServiceCollection services)
    {
        // ITelemetryService 및 TelemetryService를 Singleton으로 등록
        services.AddSingleton<ITelemetryService>(serviceProvider =>
        {
            var otelConfig = serviceProvider.GetRequiredService<IOptions<OtelConfig>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<TelemetryService>>();

            return new TelemetryService(
                serviceName: otelConfig.ServiceName,
                serviceVersion: otelConfig.ServiceVersion,
                logger: logger
            );
        });

        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddHostedService<SodBackgroundWorker>();

        return services;
    }
}
