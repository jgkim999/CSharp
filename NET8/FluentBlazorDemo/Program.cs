using System.Configuration;
using Microsoft.FluentUI.AspNetCore.Components;
using FluentBlazorDemo.Components;
using Serilog;
using WebDemo.Domain.Configs;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpClient();
        
        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddFluentUIComponents();

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        
        // Add support to logging with SERILOG
        builder.Host.UseSerilog(Log.Logger);

        DbConfig? dbConfig = builder.Configuration.GetSection("DB").Get<DbConfig>();
        if (dbConfig is null)
        {
            Log.Logger.Fatal($"Read failed, DbConfig");
        }
        Log.Logger.Information($"DbConfig:{dbConfig?.AccountDb}");
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        
        // Add support to logging request with SERILOG
        app.UseSerilogRequestLogging();
        
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
