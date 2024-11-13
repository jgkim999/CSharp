using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace WebDemo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string name, string version)
    {
        //services.AddAutoMapper(Assembly.GetExecutingAssembly());

        //services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        /*
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        });
        */
        ActivityManager activityManager = new ActivityManager(name, version);
        services.AddSingleton<ActivityManager>(activityManager);
        
        return services;
    }
}