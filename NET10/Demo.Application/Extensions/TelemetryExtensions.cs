using Demo.Application.Decorators;
using Demo.Application.Services;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Demo.Application.Extensions;

/// <summary>
/// 텔레메트리 관련 확장 메서드
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// LiteBus 명령 핸들러에 텔레메트리 데코레이터를 추가합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <summary>
    /// Adds telemetry decorators to all LiteBus command handlers registered in the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The modified service collection with telemetry integration for command handlers.</returns>
    public static IServiceCollection AddLiteBusTelemetry(this IServiceCollection services)
    {
        // TelemetryService가 등록되어 있는지 확인
        //services.TryAddSingleton<ITelemetryService>();
        // 기존 명령 핸들러들을 데코레이터로 감싸기
        DecorateCommandHandlers(services);

        return services;
    }

    /// <summary>
    /// 등록된 명령 핸들러들을 텔레메트리 데코레이터로 감쌉니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    private static void DecorateCommandHandlers(IServiceCollection services)
    {
        var handlerDescriptors = services
            .Where(descriptor => IsCommandHandler(descriptor.ServiceType))
            .ToList();

        foreach (var descriptor in handlerDescriptors)
        {
            var serviceType = descriptor.ServiceType;
            var implementationType = descriptor.ImplementationType;

            if (implementationType == null) continue;

            // 기존 서비스 제거
            services.Remove(descriptor);

            // 원본 핸들러를 다른 이름으로 등록
            var originalServiceType = typeof(ICommandHandler<,>).MakeGenericType(
                serviceType.GetGenericArguments());
            
            services.Add(new ServiceDescriptor(
                implementationType,
                implementationType,
                descriptor.Lifetime));

            // 데코레이터 등록
            if (IsResultCommandHandler(serviceType))
            {
                var commandType = serviceType.GetGenericArguments()[0];
                var resultType = serviceType.GetGenericArguments()[1];
                var decoratorType = typeof(TelemetryCommandHandlerDecorator<,>)
                    .MakeGenericType(commandType, resultType);

                services.Add(new ServiceDescriptor(
                    serviceType,
                    provider => CreateDecorator(provider, decoratorType, implementationType),
                    descriptor.Lifetime));
            }
            else if (IsVoidCommandHandler(serviceType))
            {
                var commandType = serviceType.GetGenericArguments()[0];
                var decoratorType = typeof(TelemetryCommandHandlerDecorator<>)
                    .MakeGenericType(commandType);

                services.Add(new ServiceDescriptor(
                    serviceType,
                    provider => CreateDecorator(provider, decoratorType, implementationType),
                    descriptor.Lifetime));
            }
        }
    }

    /// <summary>
    /// 데코레이터 인스턴스를 생성합니다.
    /// </summary>
    /// <summary>
    /// Instantiates a telemetry decorator for a command handler, resolving its dependencies from the service provider.
    /// </summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="decoratorType">The type of the telemetry decorator to create.</param>
    /// <param name="innerHandlerType">The type of the inner command handler to be wrapped.</param>
    /// <returns>An instance of the specified decorator type wrapping the inner handler.</returns>
    private static object CreateDecorator(IServiceProvider provider, Type decoratorType, Type innerHandlerType)
    {
        var innerHandler = provider.GetRequiredService(innerHandlerType);
        return Activator.CreateInstance(decoratorType, innerHandler, 
            provider.GetRequiredService(typeof(Microsoft.Extensions.Logging.ILogger<>).MakeGenericType(decoratorType)),
            provider.GetRequiredService<ITelemetryService>())!;
    }

    /// <summary>
    /// 타입이 명령 핸들러인지 확인합니다.
    /// </summary>
    /// <param name="type">확인할 타입</param>
    /// <returns>명령 핸들러 여부</returns>
    private static bool IsCommandHandler(Type type)
    {
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
    }

    /// <summary>
    /// 타입이 결과를 반환하는 명령 핸들러인지 확인합니다.
    /// </summary>
    /// <param name="type">확인할 타입</param>
    /// <returns>결과 반환 명령 핸들러 여부</returns>
    private static bool IsResultCommandHandler(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(ICommandHandler<,>);
    }

    /// <summary>
    /// 타입이 void 명령 핸들러인지 확인합니다.
    /// </summary>
    /// <param name="type">확인할 타입</param>
    /// <returns>void 명령 핸들러 여부</returns>
    private static bool IsVoidCommandHandler(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(ICommandHandler<>) &&
               type.GetGenericArguments().Length == 1;
    }
}