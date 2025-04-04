using Microsoft.Extensions.Logging;

using Spectre.Console;

using Unity.Tools.Models.Sqlite;
using Unity.Tools;
using Unity.Tools.Repositories;

using ConfigOption = Unity.Tools.ConfigOption;

namespace SpectreDemo.Command;

public class BuildDbCompareCommand
{
    private ILogger _logger;
    private ConfigOption _option;
    private CancellationTokenSource _cancellationTokenSource;

    public BuildDbCompareCommand(
        ConfigOption option,
        ILogger logger,
        CancellationTokenSource cancellationTokenSource)
    {
        _logger = logger;
        _option = option;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public async Task ExecuteAsync()
    {
        string srcDbFileName = string.Empty;
        string targetDbFileName = string.Empty;

        var dbFiles = Directory.GetFiles(_option.BuildCacheDbPath);

        srcDbFileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("소스 [green]DB 파일을 선택하세요 [/]?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down)[/]")
                .AddChoices(dbFiles));

        targetDbFileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("비교할 타겟 [green]DB 파일을 선택하세요 [/]?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down)[/]")
                .AddChoices(dbFiles));

        var table = new Table();

        // Add some columns
        table.AddColumn("Section");
        table.AddColumn("Value");

        // Add rows
        table.AddRow("Source", $"[green]{srcDbFileName}[/]");
        table.AddRow("Target", $"[green]{targetDbFileName}[/]");
        AnsiConsole.Write(table);

        IBuildCacheDb srcDb = new SqliteBuildCacheDb(srcDbFileName, _logger);
        IBuildCacheDb targetDb = new SqliteBuildCacheDb(targetDbFileName, _logger);

        var srcFiles = await srcDb.GetFilesAsync();
        var targetFiles = await targetDb.GetFilesAsync();

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
                    var checkTask = ctx.AddTask("[green]Check[/]");
                    SpectreProgressContext checkTaskProgress = new(checkTask);


                    checkTaskProgress.SetMaxValue(srcFiles.Count + targetFiles.Count);

                    foreach (var srcFile in srcFiles)
                    {
                        checkTaskProgress.Increment(1);
                    }

                    foreach (var targetFile in targetFiles)
                    {
                        checkTaskProgress.Increment(1);
                    }
                    checkTaskProgress.StopTask();
                });
        AnsiConsole.MarkupLine("[green]비교가 완료되었습니다.[/]");
        AnsiConsole.Console.Input.ReadKey(false);
    }
}
