using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using UnityUtilGui.ViewModels;

namespace UnityUtilGui.Views
{
    public partial class AssetDbMakeView : UserControl
    {
        public AssetDbMakeView()
        {
            InitializeComponent();
        }

        private async void AssetDirectoryButton_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                TopLevel? topLevel = TopLevel.GetTopLevel(this);
                Debug.Assert(topLevel != null);

                IReadOnlyList<IStorageFolder> folder = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions());
                if (folder.Count == 0)
                {
                    return;
                }

                App.Logger!.LogInformation("Asset selected folder {Folder}", folder[0].Path);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AssetDbMakeViewModel? vm = DataContext as AssetDbMakeViewModel;
                    Debug.Assert(vm != null, nameof(vm) + " == null");
                    vm.AssetDirectory = folder[0].Path.AbsolutePath;
                });
            }
            catch (Exception exception)
            {
                App.Logger!.LogError(exception, "Failed to open folder");
            }
        }

        private async void AssetDbDirectoryButton_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                TopLevel? topLevel = TopLevel.GetTopLevel(this);
                Debug.Assert(topLevel != null);

                IReadOnlyList<IStorageFolder> folder = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions()).ConfigureAwait(false);
                if (folder.Count == 0)
                {
                    return;
                }

                App.Logger!.LogInformation("Asset DB selected folder {Folder}", folder[0].Path);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AssetDbMakeViewModel? vm = DataContext as AssetDbMakeViewModel;
                    Debug.Assert(vm != null, nameof(vm) + " == null");
                    vm.AssetDbDirectory = folder[0].Path.AbsolutePath;
                });
            }
            catch (Exception exception)
            {
                App.Logger!.LogError(exception, "Failed to open folder");
            }
        }
    }
}