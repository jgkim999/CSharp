using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Linq;

using Unity.Tools;

using UnityUtilGui.ViewModels;

namespace UnityUtilGui.Views;

public partial class AssetDbSearchView : UserControl
{
    public AssetDbSearchView()
    {
        InitializeComponent();
    }

    public static FilePickerFileType DbAll { get; } = new("All DB")
    {
        Patterns = new[] { "*.sqlite", "*.db" },
        AppleUniformTypeIdentifiers = new[] { "public.item", "public" },
        MimeTypes = new[] { "image/*" }
    };

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            Debug.Assert(topLevel != null);

            var configOption = App.Services?.GetService<ConfigOption>();
            Debug.Assert(configOption != null, nameof(configOption) + " == null");
            var startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(configOption.AssetDbPath);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions()
                {
                    SuggestedStartLocation = startLocation,
                    FileTypeFilter = [DbAll],
                    AllowMultiple = false
                }).ConfigureAwait(false);
            if (files.Count == 0)
            {
                return;
            }

            App.Logger!.LogInformation("Asset DB selected file  {File}", files[0].Path);
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AssetDbSearchViewModel? vm = DataContext as AssetDbSearchViewModel;
                vm?.SetDbFilePath(files[0].Path.AbsolutePath);   
            });
        }
        catch (Exception exception)
        {
            App.Logger!.LogError(exception, "Failed to open folder");
        }
    }
}
