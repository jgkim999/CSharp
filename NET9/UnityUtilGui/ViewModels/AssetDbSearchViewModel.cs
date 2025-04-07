using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;

using Unity.Tools;

namespace UnityUtilGui.ViewModels;

public partial class AssetDbSearchViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    
    [ObservableProperty] string _assetDbDirectory;
    [ObservableProperty] string _dbFilePath = String.Empty;

    public AssetDbSearchViewModel(IServiceProvider services)
    {
        _services = services;
        ConfigOption? configOption = _services.GetService<ConfigOption>();
        _assetDbDirectory = configOption?.AssetDbPath ?? string.Empty;
    }

    public void SetDbFilePath(string newValue)
    {
        App.Logger?.LogInformation("Changed {Value}", newValue);
        DbFilePath = newValue;
    }
}
