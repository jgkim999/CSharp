using Demo.Application.ErrorHandlers;
using LiteBus.Commands.Abstractions;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Events.Abstractions;
using LiteBus.Events.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using LiteBus.Queries.Abstractions;
using LiteBus.Queries.Extensions.MicrosoftDependencyInjection;

namespace Demo.SimpleSocket;

/// <summary>
/// LiteBus 메시징 시스템 초기화를 담당하는 정적 클래스
/// CQRS 패턴의 Command, Query, Event 처리를 위한 중재자(Mediator) 설정을 제공합니다
/// </summary>
public static class LiteBusInitializer
{
    /// <summary>
    /// WebApplicationBuilder에 LiteBus 메시징 시스템을 추가하고 구성합니다
    /// 모든 로드된 어셈블리에서 Command, Query, Event 핸들러를 자동으로 등록하며
    /// 각 타입별 Pre/Post/Error 핸들러를 설정합니다
    /// </summary>
    /// <param name="appBuilder">웹 애플리케이션 빌더</param>
    public static void AddLiteBusApplication(this WebApplicationBuilder appBuilder)
    {
        // Handler 등록
        appBuilder.Services.AddTransient<IQueryErrorHandler, QueryErrorHandler>();
        appBuilder.Services.AddTransient<IQueryPreHandler, QueryPreHandler>();
        appBuilder.Services.AddTransient<IQueryPostHandler, QueryPostHandler>();
    
        appBuilder.Services.AddTransient<ICommandErrorHandler, CommandErrorHandler>();
        appBuilder.Services.AddTransient<ICommandPreHandler, CommandPreHandler>();
        appBuilder.Services.AddTransient<ICommandPostHandler, CommandPostHandler>();
    
        appBuilder.Services.AddTransient<IEventErrorHandler, EventErrorHandler>();
        appBuilder.Services.AddTransient<IEventPreHandler, EventPreHandler>();
        appBuilder.Services.AddTransient<IEventPostHandler, EventPostHandler>();
    
        // 모든 로드된 Assembly 가져오기
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        appBuilder.Services.AddLiteBus(liteBus =>
        {
            liteBus.AddCommandModule(module =>
            {
                foreach (var assembly in assemblies)
                {
                    module.RegisterFromAssembly(assembly);
                }
            });

            liteBus.AddQueryModule(module =>
            {
                foreach (var assembly in assemblies)
                {
                    module.RegisterFromAssembly(assembly);
                }
            });

            liteBus.AddEventModule(module =>
            {
                foreach (var assembly in assemblies)
                {
                    module.RegisterFromAssembly(assembly);
                }
            });
        });
    }
}