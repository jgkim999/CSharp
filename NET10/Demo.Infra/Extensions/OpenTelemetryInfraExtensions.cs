using Demo.Application.Configs;
using Demo.Infra.Services;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Demo.Infra.Extensions;

/// <summary>
/// OpenTelemetry Infrastructure 레이어 설정을 위한 확장 메서드
/// </summary>
public static class OpenTelemetryInfraExtensions
{
    /// <summary>
    /// OpenTelemetry의 인프라 관련 계측 및 익스포터를 구성합니다
    /// </summary>
    /// <param name="builder">OpenTelemetryBuilder 인스턴스</param>
    /// <param name="config">OpenTelemetry 구성 설정</param>
    /// <returns>구성된 OpenTelemetryBuilder</returns>
    public static OpenTelemetryBuilder AddOpenTelemetryInfrastructure(this OpenTelemetryBuilder builder, OtelConfig config)
    {
        StackExchangeRedisInstrumentation? redisInstrumentation = null;
        
        // TODO: GamePulseActivitySource 초기화
        GamePulseActivitySource.Initialize(config.ServiceName, config.ServiceVersion);

        // 추적 인프라 설정
        builder.WithTracing(tracing =>
        {
            // Redis 계측
            tracing.AddRedisInstrumentation()
                .ConfigureRedisInstrumentation(instrumentation => redisInstrumentation = instrumentation);
        });

        // Redis 계측 서비스 등록
        if (redisInstrumentation is not null)
        {
            builder.Services.AddSingleton(redisInstrumentation);
        }
        return builder;
    }
}
