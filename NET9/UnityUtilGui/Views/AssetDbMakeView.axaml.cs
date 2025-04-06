using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions());
            App.Logger!.LogInformation("Selected folder {Folder}", folder);
        }

        private async void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                TopLevel? topLevel = TopLevel.GetTopLevel(this);
                Debug.Assert(topLevel != null);
            
                IReadOnlyList<IStorageFolder> folder = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions());
                if (folder.Count == 0)
                    return;
                App.Logger!.LogInformation("Selected folder {Folder}", folder[0].Path);
                AssetDbMakeViewModel vm = (AssetDbMakeViewModel)this.DataContext!;
                vm.SelectedFolder = folder[0].Path.ToString();
            }
            catch (Exception exception)
            {
                App.Logger!.LogError(exception, "Failed to open folder");
            }
        }
    }
}
