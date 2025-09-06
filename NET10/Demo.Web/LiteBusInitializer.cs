using Demo.Application.ErrorHandlers;
using LiteBus.Commands.Abstractions;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Events.Abstractions;
using LiteBus.Events.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using LiteBus.Queries.Abstractions;
using LiteBus.Queries.Extensions.MicrosoftDependencyInjection;

namespace Demo.Web;

public static class LiteBusInitializer
{
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