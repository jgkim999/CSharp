using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;

using Unity.Tools;

namespace UnityUtilGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Nwz Unity Tool";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AssetDbMakeIsActive))]
    [NotifyPropertyChangedFor(nameof(AssetDbSearchIsActive))]
    [NotifyPropertyChangedFor(nameof(AssetDbViewIsActive))]
    private ViewModelBase _currentPage;
    
    private AssetDbMakeViewModel _assetDbMakePage;
    private AssetDbSearchViewModel _assetDbSearchPage;
    private AssetDbViewModel _assetDbViewPage;

    public bool AssetDbMakeIsActive => CurrentPage == _assetDbMakePage;
    public bool AssetDbSearchIsActive => CurrentPage == _assetDbSearchPage;
    public bool AssetDbViewIsActive => CurrentPage == _assetDbViewPage;

    private readonly IServiceProvider _services;

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        var option = _services.GetService<ConfigOption>();
        
        _assetDbMakePage = new AssetDbMakeViewModel(_services);
        _assetDbSearchPage = new AssetDbSearchViewModel(_services);
        _assetDbViewPage = new AssetDbViewModel(_services);
    
        CurrentPage = _assetDbMakePage;
        
        App.Logger!.LogInformation("Hello Log");
    }
    
    [RelayCommand]
    public void GoToAssetDbMakePage() => CurrentPage = _assetDbMakePage;
    [RelayCommand]
    public void GoToAssetDbSearchPage() => CurrentPage = _assetDbSearchPage;
    [RelayCommand]
    public void GoToAssetDbViewPage() => CurrentPage = _assetDbViewPage;
}
