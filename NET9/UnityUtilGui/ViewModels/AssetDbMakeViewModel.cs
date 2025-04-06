using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Threading.Tasks;

using Unity.Tools;

namespace UnityUtilGui.ViewModels;

public partial class AssetDbMakeViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    
    [ObservableProperty]
    string _selectedFolder;

    public AssetDbMakeViewModel(IServiceProvider services)
    {
        _services = services;
        var configOption = _services.GetService<ConfigOption>();
        _selectedFolder = configOption?.AssetPath ?? string.Empty;
    }
}
