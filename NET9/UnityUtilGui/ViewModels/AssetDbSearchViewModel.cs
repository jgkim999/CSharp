using System;

namespace UnityUtilGui.ViewModels;

public partial class AssetDbSearchViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    public AssetDbSearchViewModel(IServiceProvider services)
    {
        _services = services;
    }
}