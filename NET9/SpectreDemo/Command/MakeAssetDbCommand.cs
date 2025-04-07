using Microsoft.Extensions.Logging;

using Spectre.Console;

using System.Collections.Concurrent;

using Unity.Tools;
using Unity.Tools.Models;
using Unity.Tools.Repositories;

namespace SpectreDemo.Command;

internal class MakeAssetDbCommand
{
    private readonly ConfigOption _option;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public MakeAssetDbCommand(ConfigOption option, ILogger logger, CancellationTokenSource cancellationTokenSource)
    {
        _option = option;
        _logger = logger;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async Task ExecuteAsync()
    {
        Dictionary<int, string> directoryMap = new();
        ConcurrentBag<UnityMetaFileInfo> assetFiles = new();

        await AnsiConsole.Status()
            .StartAsync("[green]1/5 Search Directories[/]", async ctx =>
            {
                List<string> directories = await AssetSearch.DirectorySearchAsync(
                    _option.AssetPath,
                    _logger,
                    _option.IgnoreDirectoryNames);
                for (int i = 1; i <= directories.Count; ++i)
                {
                    directoryMap.Add(i, directories[i - 1]);
                }
            });

        await AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn(),
                    new SpinnerColumn()
                })
                .StartAsync(async ctx =>
                {
                    // Define tasks
                    var metaListTask = ctx.AddTask("[green]2/5 Make meta guid[/]");
                    SpectreProgressContext metaListTaskProgress = new(metaListTask);

                    var metaGuidTask = ctx.AddTask("[green]3/5 Find Meta's guid[/]");
                    SpectreProgressContext metaGuidTaskProgress = new(metaGuidTask);

                    var assetDependencyTask = ctx.AddTask("[green]4/5 Find guid dependency[/]");
                    SpectreProgressContext assetDependencyTaskProgress = new(assetDependencyTask);

                    var sqliteTask = ctx.AddTask("[green]5/5 Make Sqlite DB[/]");
                    SpectreProgressContext sqliteTaskProgress = new(sqliteTask);

                    assetFiles = await AssetSearch.MakeMetaListAsync(
                        directoryMap,
                        _logger,
                        metaListTaskProgress);
                    
                    await AssetSearch.MakeMetaGuidAsync(
                        directoryMap,
                        assetFiles,
                        _logger,
                        metaGuidTaskProgress,
                        _cancellationTokenSource.Token);
                    
                    await AssetSearch.MakeDependencyAsync(
                        directoryMap,
                        assetFiles,
                        _option.FileExtAnalyze,
                        _option.IgnoreGuids,
                        _logger,
                        assetDependencyTaskProgress,
                        _cancellationTokenSource.Token);
                    
                    if (Directory.Exists(_option.AssetDbPath) == false)
                    {
                        Directory.CreateDirectory(_option.AssetDbPath);
                    }

                    sqliteTaskProgress.SetMaxValue(directoryMap.Count + assetFiles.Count);
                    var dbPath = Path.Combine(_option.AssetDbPath, $"DependencyDb-{DateTime.Now:yyyyMMdd-HHmm}.sqlite");
                    IDependencyDb dependencyDb = new SqliteDependencyDb(dbPath, _logger);

                    await dependencyDb.AddDirectoryAsync(directoryMap, sqliteTaskProgress);
                    await dependencyDb.AddAssetAsync(assetFiles, sqliteTaskProgress);

                    sqliteTaskProgress.StopTask();
                    AnsiConsole.MarkupLine($"[green]DB 생성이 완료되었습니다. {dbPath}[/]");
                    AnsiConsole.Console.Input.ReadKey(false);
                });
    }
}
