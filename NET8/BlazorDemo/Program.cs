using BlazorDemo.Components;
using StackExchange.Redis;
using WebDemo.Application.Interfaces;
using WebDemo.Infra;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorBootstrap();

ConfigurationOptions redisOption = new()
{
    EndPoints = 
    {
        { "localhost", 6379 }
    }
};
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisOption);
builder.Services.AddSingleton<IPublisher>(new RedisPublisher(redis));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
