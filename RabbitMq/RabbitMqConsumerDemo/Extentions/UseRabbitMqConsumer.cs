using ConsumerDemo1.Services;

namespace ConsumerDemo1.Extentions;

public static class UseRabbitMqConsumerExtentions
{
    public static Consumer Listener { get; set; }
    
    public static void ConfigureRabbitMqConsumer(IServiceCollection services)
    {
        services.AddHostedService<Consumer>();
    }
    
    public static IApplicationBuilder UseRabbitListener(this IApplicationBuilder app)
    {
        Listener = app.ApplicationServices.GetService<Consumer>();
        var life = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        life.ApplicationStarted.Register(OnStarted);
        return app;
    }

    private static void OnStarted()
    {
        Listener.Receive();
    }
}
