using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

using System.Diagnostics;
using System.IO;
using System.Linq;

using Unity.Tools;

using UnityUtilGui.ViewModels;
using UnityUtilGui.Views;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UnityUtilGui;

public partial class App : Application
{
    public static ILogger? Logger;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Build a configuration object from JSON file
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        Debug.Assert(configuration != null, nameof(configuration) + " == null");

        ConfigOption? configOption = configuration
            .GetSection("ConfigOption")
            .Get<ConfigOption>();
        Debug.Assert(configOption != null, nameof(configOption) + " == null");
        
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        
        Logger = new SerilogLoggerFactory(logger)
            .CreateLogger<Application>();

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddLogging(x => x.AddSerilog(logger));
        collection.AddSingleton<ConfigOption>(configOption);

        ServiceProvider services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel(services), };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}