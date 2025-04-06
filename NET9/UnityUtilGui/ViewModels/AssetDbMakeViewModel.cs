using Microsoft.Extensions.DependencyInjection;

using System;

using Unity.Tools;

namespace UnityUtilGui.ViewModels;

public partial class AssetDbMakeViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    public AssetDbMakeViewModel(IServiceProvider services)
    {
        _services = services;
        var configOption = _services.GetService<ConfigOption>();
    }
}