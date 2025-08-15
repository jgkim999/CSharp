using Demo.Application.Services.Sod;
using Demo.Infra.Services.Sod;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infra.Extensions;

/// <summary>
/// SOD 인프라 서비스 등록을 위한 확장 메서드
/// </summary>
public static class SodInfraExtensions
{
    /// <summary>
    /// SOD 백그라운드 작업 큐 및 워커 서비스를 의존성 주입 컨테이너에 등록합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>업데이트된 IServiceCollection 인스턴스</returns>
    public static IServiceCollection AddSodInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddHostedService<SodBackgroundWorker>();

        return services;
    }
}