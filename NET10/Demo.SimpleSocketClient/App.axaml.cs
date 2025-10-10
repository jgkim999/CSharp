using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Demo.SimpleSocketClient.ViewModels;
using Demo.SimpleSocketClient.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demo.SimpleSocketClient;

public partial class App : Avalonia.Application
{
    private readonly IServiceProvider? _serviceProvider;

    public App() : this(null)
    {
    }

    public App(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // ServiceProvider에서 ILoggerFactory 가져오기
            var loggerFactory = _serviceProvider?.GetService<ILoggerFactory>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(loggerFactory),
            };
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