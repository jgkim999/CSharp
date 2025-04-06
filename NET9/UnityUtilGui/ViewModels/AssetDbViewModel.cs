using System;

namespace UnityUtilGui.ViewModels;

public partial class AssetDbViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    public AssetDbViewModel(IServiceProvider services)
    {
        _services = services;
    }
}
