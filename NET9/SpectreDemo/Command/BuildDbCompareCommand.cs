using Microsoft.Extensions.Logging;

using Spectre.Console;

using System.IO;

using Unity.Tools.Models.Sqlite;
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

        var dirFiles = Directory.GetFiles(_option.BuildCacheDbPath);
        if (dirFiles.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]DB 파일이 없습니다.[/]");
            AnsiConsole.Console.Input.ReadKey(false);
            return;
        }
        var dbFileNames = new List<string>();
        foreach (var dirFile in dirFiles)
        {
            if (Path.GetExtension(dirFile) == ".sqlite")
                dbFileNames.Add(dirFile);
        }

        srcDbFileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("소스 [green]DB 파일을 선택하세요 [/]?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down)[/]")
                .AddChoices(dbFileNames));

        targetDbFileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("비교할 타겟 [green]DB 파일을 선택하세요 [/]?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down)[/]")
                .AddChoices(dbFileNames));

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

        List<BuildCacheFileDirectory> deleteFiles = new();
        List<BuildCacheFileDirectory> newFiles = new();
        List<(BuildCacheFileDirectory src, BuildCacheFileDirectory target)> updateFiles = new();

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
                        List<BuildCacheFileDirectory> rows = await targetDb.GetFilesAsync(srcFile.Filename);
                        if (rows.Count == 0)
                        {
                            // Delete
                            deleteFiles.Add(srcFile);
                        }
                        else if (rows.Count == 1 && rows[0].IsEqual(srcFile) == false)
                        {
                            // Update
                            updateFiles.Add((srcFile, rows[0]));
                        }
                        else
                        {
                            foreach (var row in rows)
                            {
                                if (row.IsEqual(srcFile) == false)
                                {
                                    // Update
                                    updateFiles.Add((srcFile, row));
                                }
                            }
                        }
                    }

                    foreach (var targetFile in targetFiles)
                    {
                        checkTaskProgress.Increment(1);
                        // New
                        var rows = await srcDb.GetFilesAsync(targetFile.Filename);
                        if (rows.Count == 0)
                        {
                            newFiles.Add(targetFile);
                        }
                    }
                    checkTaskProgress.StopTask();
                });
        
        string diffFileName = Path.Combine(_option.BuildCacheDbPath, $"Diff-{DateTime.Now:yyyyMMdd-HHmm}.csv");
        await using (StreamWriter writeText = new(diffFileName))
        {
            await writeText.WriteLineAsync("Type,Path,Length,CreationTimeUtc,LastWriteTimeUtc,LastAccessTimeUtc");
            foreach (BuildCacheFileDirectory file in deleteFiles)
            {
                string path = Path.Combine(file.Dirname, file.Filename);
                await writeText.WriteLineAsync(
                    $"del,{path},{file.Length},'{file.CreationTimeUtc}','{file.LastWriteTimeUtc}','{file.LastAccessTimeUtc}'");
            }

            foreach (BuildCacheFileDirectory file in newFiles)
            {
                string path = Path.Combine(file.Dirname, file.Filename);
                await writeText.WriteLineAsync(
                    $"new,{path},{file.Length},'{file.CreationTimeUtc}','{file.LastWriteTimeUtc}','{file.LastAccessTimeUtc}'");
            }

            foreach (var (src, target) in updateFiles)
            {
                string srcPath = Path.Combine(src.Dirname, src.Filename);
                string targetPath = Path.Combine(target.Dirname, target.Filename);
                await writeText.WriteLineAsync(
                    $"update src,{srcPath},{src.Length},'{src.CreationTimeUtc}','{src.LastWriteTimeUtc}','{src.LastAccessTimeUtc}'");
                await writeText.WriteLineAsync(
                    $"update target,{targetPath},{target.Length},'{target.CreationTimeUtc}','{target.LastWriteTimeUtc}','{target.LastAccessTimeUtc}'");
            }
        }
        
        AnsiConsole.MarkupLine($"[green]비교 파일이 생성 완료되었습니다. {diffFileName}[/]");
        AnsiConsole.Console.Input.ReadKey(false);
    }
}
