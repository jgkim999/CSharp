using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Unity.Tools;

using UnityUtilGui.ViewModels;

namespace UnityUtilGui.Views
{
    public partial class AssetDbMakeView : UserControl
    {
        public AssetDbMakeView()
        {
            InitializeComponent();
        }

        public async Task OpenFolderAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            Debug.Assert(topLevel != null);

            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions());
            App.Logger!.LogInformation("Selected folder {Folder}", folder);
        }

        private async void AssetDirectoryButton_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var configOption = App.Services?.GetService<ConfigOption>();
                    
                TopLevel? topLevel = TopLevel.GetTopLevel(this);
                Debug.Assert(topLevel != null);

                IReadOnlyList<IStorageFolder> folder = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions());
                if (folder.Count == 0)
                {

                    return;
                }
                App.Logger!.LogInformation("Asset selected folder {Folder}", folder[0].Path);
                AssetDbMakeViewModel vm = (AssetDbMakeViewModel)DataContext!;
                vm.AssetDirectory = folder[0].Path.AbsolutePath;
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
                    new FolderPickerOpenOptions());
                if (folder.Count == 0)
                {
                    return;
                }
                App.Logger!.LogInformation("Asset DB selected folder {Folder}", folder[0].Path);
                AssetDbMakeViewModel vm = (AssetDbMakeViewModel)DataContext!;
                vm.AssetDbDirectory = folder[0].Path.AbsolutePath;
            }
            catch (Exception exception)
            {
                App.Logger!.LogError(exception, "Failed to open folder");
            }
        }
    }
}