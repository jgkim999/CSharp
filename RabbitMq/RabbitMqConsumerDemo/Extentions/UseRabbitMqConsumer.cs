using ConsumerDemo1.Services;
using RabbitMq.Common;

namespace ConsumerDemo1.Extentions;

public static class UseRabbitMqConsumerExtentions
{
    public static ConsumerBase Listener { get; set; }
    
    public static void ConfigureRabbitMqConsumer(IServiceCollection services)
    {
        services.AddHostedService<ConsumerBase>();
    }
    
    public static IApplicationBuilder UseRabbitListener(this IApplicationBuilder app)
    {
        Listener = app.ApplicationServices.GetService<ConsumerBase>();
        var life = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        life.ApplicationStarted.Register(OnStarted);
        return app;
    }

    private static void OnStarted()
    {
        Listener.Receive();
    }
}
