using Demo.Application.Configs;
using Demo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Application.Extensions;

/// <summary>
/// SOD 서비스 등록을 위한 확장 메서드
/// </summary>
public static class SodServiceExtensions
{
    /// <summary>
    /// SOD 텔레메트리 서비스를 의존성 주입 컨테이너에 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <summary>
    /// Registers the telemetry implementation (ITelemetryService) as a singleton using values from OtelConfig.
    /// </summary>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// The factory reads <see cref="OtelConfig"/> via <see cref="IOptions{OtelConfig}"/> and resolves
    /// <see cref="ILogger{TelemetryService}"/> to construct a <see cref="TelemetryService"/> instance
    /// with the configured service name and version.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the required services (IOptions&lt;OtelConfig&gt; or ILogger&lt;TelemetryService&gt;) are not registered.
    /// </exception>
    public static IServiceCollection AddSodServices(this IServiceCollection services)
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

        return services;
    }
}