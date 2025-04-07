using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Unity.Tools;
using Unity.Tools.Repositories;

namespace UnityUtilGui.ViewModels;

public partial class AssetDbMakeViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    
    [ObservableProperty] string _assetDirectory;
    [ObservableProperty] private string _assetDbDirectory;
    [ObservableProperty] private bool _isDirectorySearchBusy = false;
    
    [ObservableProperty] private int _findMetaMaxValue = 100;
    [ObservableProperty] private int _findMetaProgressValue = 0;
    
    [ObservableProperty] private int _findGuidMaxValue = 100;
    [ObservableProperty] private int _findGuidProgressValue = 0;
    
    [ObservableProperty] private int _analyzeMetaMaxValue = 100;
    [ObservableProperty] private int _analyzeMetaProgressValue = 0;
    
    [ObservableProperty] private int _makeDbMaxValue = 100;
    [ObservableProperty] private int _makeDbProgressValue = 0;

    [ObservableProperty] private string _dbPath = "DB가 생성되면 경로가 표시됩니다";
    
    private object _findMetaLock = new object();
    private bool _canExecute = true;
    
    public AssetDbMakeViewModel(IServiceProvider services)
    {
        _services = services;
        ConfigOption? configOption = _services.GetService<ConfigOption>();
        _assetDirectory = configOption?.AssetPath ?? string.Empty;
        _assetDbDirectory = configOption?.AssetDbPath ?? string.Empty;
    }

    bool CanExecute()
    {
        return _canExecute;
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    public async Task MakeDb()
    {
        ConfigOption? configOption = _services.GetService<ConfigOption>();
        Debug.Assert(configOption != null, nameof(configOption) + " == null");
        if (App.Logger == null)
            return;
        IsDirectorySearchBusy = true;
        List<string> directories = await AssetSearch.DirectorySearchAsync(AssetDirectory, App.Logger, configOption.IgnoreDirectoryNames).ConfigureAwait(false);
        IsDirectorySearchBusy = false;
       
        if (directories.Count == 0)
        {
            App.Logger.LogInformation("폴더가 없습니다. {BaseDir}", AssetDirectory);
            var box = MessageBoxManager.GetMessageBoxStandard(
                "경고",
                "하위 폴더가 존재하지 않습니다.",
                ButtonEnum.Ok);

            var result = await box.ShowAsync();
            return;
        }

        _canExecute = false;
        
        FindMetaMaxValue = directories.Count;
        Dictionary<int, string> directoryDic = new();
        int dirNum = 0;
        foreach (var directory in directories)
        {
            directoryDic.Add(++dirNum, directory);
        }
        
        IProgressContext metaListProgress = new AvaloniaProgressContext(
            maxAction: d => { FindMetaMaxValue = d; },
            incAction: d => {
                lock (_findMetaLock)
                {
                    int progressValue = FindMetaProgressValue;
                    FindMetaProgressValue = Interlocked.Add(ref progressValue, d);    
                }
            },
            statusAction: s => { App.Logger.LogInformation(s); },
            startTask: () => { App.Logger.LogInformation("start"); },
            stopTask: () => { App.Logger.LogInformation("stop"); }
        );
        var metaFiles = await AssetSearch.MakeMetaListAsync(directoryDic, App.Logger, metaListProgress).ConfigureAwait(false);

        FindGuidMaxValue = metaFiles.Count;
        IProgressContext metaYamlProgress = new AvaloniaProgressContext(
            maxAction: d => { FindGuidMaxValue = d; },
            incAction: d => {
                lock (_findMetaLock)
                {
                    int progressValue = FindGuidProgressValue;
                    FindGuidProgressValue = Interlocked.Add(ref progressValue, d);    
                }
            },
            statusAction: s => { App.Logger.LogInformation(s); },
            startTask: () => { App.Logger.LogInformation("start"); },
            stopTask: () => { App.Logger.LogInformation("stop"); }
        );
        await AssetSearch.MakeMetaGuidAsync(directoryDic, metaFiles, App.Logger, metaYamlProgress).ConfigureAwait(false);

        AnalyzeMetaMaxValue = metaFiles.Count;
        IProgressContext yamlAnalyzeProgress = new AvaloniaProgressContext(
            maxAction: d => { AnalyzeMetaMaxValue = d; },
            incAction: d => {
                lock (_findMetaLock)
                {
                    int progressValue = AnalyzeMetaProgressValue;
                    AnalyzeMetaProgressValue = Interlocked.Add(ref progressValue, d);
                }
            },
            statusAction: s => { App.Logger.LogInformation(s); },
            startTask: () => { App.Logger.LogInformation("start"); },
            stopTask: () => { App.Logger.LogInformation("stop"); }
        );
        await AssetSearch.MakeDependencyAsync(directoryDic, metaFiles, configOption.FileExtAnalyze, configOption.IgnoreGuids, App.Logger, yamlAnalyzeProgress).ConfigureAwait(false);

        MakeDbMaxValue = directories.Count + metaFiles.Count;

        var dbPath = Path.Combine(AssetDbDirectory, $"DependencyDb-{DateTime.Now:yyyyMMdd-HHmm}.sqlite");
        IDependencyDb dependencyDb = new SqliteDependencyDb(dbPath, App.Logger);

        IProgressContext sqliteTaskProgress = new AvaloniaProgressContext(
            maxAction: d => { MakeDbMaxValue = d; },
            incAction: d => {
                lock (_findMetaLock)
                {
                    int progressValue = MakeDbProgressValue;
                    MakeDbProgressValue = Interlocked.Add(ref progressValue, d);
                }
            },
            statusAction: s => { App.Logger.LogInformation(s); },
            startTask: () => { App.Logger.LogInformation("start"); },
            stopTask: () => { App.Logger.LogInformation("stop"); }
        );
        await dependencyDb.AddDirectoryAsync(directoryDic, sqliteTaskProgress).ConfigureAwait(false);
        await dependencyDb.AddAssetAsync(metaFiles, sqliteTaskProgress).ConfigureAwait(false);

        DbPath = dbPath;
        
        _canExecute = true;
    } 
}
