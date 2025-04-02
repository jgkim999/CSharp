using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

using Spectre.Console;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Unity.Tools;
using Unity.Tools.Repositories;

namespace SpectreDemo;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            Debug.Assert(configuration != null, nameof(configuration) + " == null");

            Logger inner = new LoggerConfiguration()
                .ReadFrom
                .Configuration(configuration)
                .CreateLogger();

            var microsoftLogger = new SerilogLoggerFactory(inner)
                .CreateLogger<SpaceLibrary>();

            ConfigOption configOption = configuration
                .GetSection("ConfigOption")
                .Get<ConfigOption>();
            Debug.Assert(configOption != null, nameof(configOption) + " == null");
            
            var assetSearchLogger = new SerilogLoggerFactory(inner)
                .CreateLogger<AssetSearch>();
            
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            microsoftLogger.LogInformation("Directory listing creation complete.");

            ConcurrentBag<UnityMetaFileInfo> assetFiles = new();

            AnsiConsole.Progress()
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
                    var directoryTask = ctx.AddTask("[green]Search Directories[/]");
                    var metaListTask = ctx.AddTask("[green]Make meta guid[/]");
                    var metaGuidTask = ctx.AddTask("[green]Find Meta's guid[/]");
                    var assetDependencyTask = ctx.AddTask("[green]Find guid dependency[/]");
                    var sqliteTask = ctx.AddTask("[green]Make Sqlite DB[/]");
                    
                    SpectreProgressContext directoryTaskProgress = new(directoryTask);
                    SpectreProgressContext metaListTaskProgress = new(metaListTask);
                    SpectreProgressContext metaGuidTaskProgress = new(metaGuidTask);
                    SpectreProgressContext assetDependencyTaskProgress = new(assetDependencyTask);
                    SpectreProgressContext sqliteTaskProgress = new(sqliteTask);

                    List<string> directories = await AssetSearch.DirectorySearchAsync(
                        configOption.BaseDir,
                        assetSearchLogger,
                        configOption.IgnoreDirectoryNames,
                        directoryTaskProgress);

                    Dictionary<int, string> directoryMap = new();
                    for (int i = 1; i <= directories.Count; ++i)
                    {
                        directoryMap.Add(i, directories[i - 1]);
                    }
                    
                    assetFiles = await AssetSearch.MakeMetaListAsync(
                        directoryMap,
                        assetSearchLogger,
                        metaListTaskProgress);
                    
                    await AssetSearch.MetaYamlAsync(
                        directoryMap,
                        assetFiles,
                        assetSearchLogger,
                        metaGuidTaskProgress,
                        cancellationTokenSource.Token);
                    
                    await AssetSearch.YamlAnalyzeAsync(
                        directoryMap,
                        assetFiles,
                        configOption.FileExtAnalyze,
                        configOption.IgnoreGuids,
                        assetSearchLogger,
                        assetDependencyTaskProgress,
                        cancellationTokenSource.Token);

                    sqliteTaskProgress.SetMaxValue(directoryMap.Count + assetFiles.Count);
                    var dbPath = Path.Combine(configOption.DbPath, $"DependencyDb-{DateTime.Now:yyyyMMdd-HHmm}.sqlite");
                    IDependencyDb dependencyDb = new SqliteDependencyDb(dbPath, assetSearchLogger);
                    
                    dependencyDb.AddDirectory(directoryMap, sqliteTaskProgress);
                    dependencyDb.AddAsset(assetFiles, sqliteTaskProgress);

                    AnsiConsole.MarkupLine($"[green]DB File Path: {dbPath}[/]");
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            //foreach (var file in fileList)
            //{
            //    if (file.DependencyCount > 0)
            //    {
            //        AnsiConsole.MarkupLine($"File: {file.Filename} has {file.DependencyCount} dependencies.");
            //        foreach (var dependency in file.Dependencies)
            //        {
            //            AnsiConsole.MarkupLine($"Dependency: {dependency}");
            //        }
            //    }
            //}

            microsoftLogger.LogInformation("File listing creation complete.");
            Task.Delay(2000).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
 