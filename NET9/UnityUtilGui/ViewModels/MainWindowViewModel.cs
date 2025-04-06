using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace UnityUtilGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Nwz Unity Tool";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AssetDbMakeIsActive))]
    [NotifyPropertyChangedFor(nameof(AssetDbSearchIsActive))]
    [NotifyPropertyChangedFor(nameof(AssetDbViewIsActive))]
    private ViewModelBase _currentPage;
    
    private AssetDbMakeViewModel _assetDbMakePage = new AssetDbMakeViewModel();
    private AssetDbSearchViewModel _assetDbSearchPage = new AssetDbSearchViewModel();
    private AssetDbViewModel _assetDbViewPage = new AssetDbViewModel();

    public bool AssetDbMakeIsActive => CurrentPage == _assetDbMakePage;
    public bool AssetDbSearchIsActive => CurrentPage == _assetDbSearchPage;
    public bool AssetDbViewIsActive => CurrentPage == _assetDbViewPage;

    public MainWindowViewModel()
    {
        CurrentPage = _assetDbMakePage;
    }
    
    [RelayCommand]
    public void GoToAssetDbMakePage() => CurrentPage = _assetDbMakePage;
    [RelayCommand]
    public void GoToAssetDbSearchPage() => CurrentPage = _assetDbSearchPage;
    [RelayCommand]
    public void GoToAssetDbViewPage() => CurrentPage = _assetDbViewPage;
}
