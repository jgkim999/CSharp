using Microsoft.Extensions.Logging;

using Spectre.Console;

using Unity.Tools;
using Unity.Tools.Repositories;

namespace SpectreDemo.Command;

internal class MakeBuildDbCommand
{
    private readonly ConfigOption _option;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public MakeBuildDbCommand(ConfigOption option, ILogger logger, CancellationTokenSource cancellationTokenSource)
    {
        _option = option;
        _logger = logger;
        _cancellationTokenSource = cancellationTokenSource;
    }
    
    public async Task ExecuteAsync()
    {
        Dictionary<int, string> directoryMap = new();
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.BouncingBar)
            .StartAsync("[green]1/3 Search Directories[/]", async ctx =>
            {
                string[] ignore = [];
                var directories = await AssetSearch.DirectorySearchAsync(
                    _option.BuildCachePath,
                    _logger,
                    ignore);
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
                var fileTask = ctx.AddTask("[green]2/3 Search Files[/]");
                SpectreProgressContext fileTaskProgress = new(fileTask);

                var sqliteTask = ctx.AddTask("[green]3/3 Make Sqlite DB[/]");
                SpectreProgressContext sqliteTaskProgress = new(sqliteTask);

                var files = await AssetSearch.MakeBuildCacheFileListAsync(
                    directoryMap,
                    _logger,
                    fileTaskProgress);


                if (Directory.Exists(_option.BuildCacheDbPath) == false)
                {
                    Directory.CreateDirectory(_option.BuildCacheDbPath);
                }

                sqliteTaskProgress.SetMaxValue(directoryMap.Count + files.Count);

                var dbPath = Path.Combine(_option.BuildCacheDbPath, $"BuildCacheDb-{DateTime.Now:yyyyMMdd-HHmm}.sqlite");
                IBuildCacheDb buildCacheDb = new SqliteBuildCacheDb(dbPath, _logger);
                await buildCacheDb.AddDirectoryAsync(directoryMap, sqliteTaskProgress);
                await buildCacheDb.AddFileAsync(files, sqliteTaskProgress);

                AnsiConsole.MarkupLine($"[green]DB 생성이 완료되었습니다. {dbPath}[/]");
                AnsiConsole.Console.Input.ReadKey(false);
            });

        //HashFunc.GetSpookyHash("hello");
        //HashFunc.GetMd5Hash("hello");
        //await Task.CompletedTask;
    }
}
