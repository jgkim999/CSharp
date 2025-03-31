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
            
            AnsiConsole.Progress()
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
                    var task1 = ctx.AddTask("[green]Search Directories[/]");
                    var task2 = ctx.AddTask("[green]Make meta guid[/]");
                    var task3 = ctx.AddTask("[green]Find Meta's guid[/]");
                    var task4 = ctx.AddTask("[green]Find guid dependency[/]");
                    
                    SpectreProgressContext task1Progress = new(task1);
                    SpectreProgressContext task2Progress = new(task2);
                    SpectreProgressContext task3Progress = new(task3);
                    SpectreProgressContext task4Progress = new(task4);

                    List<string> directories = await AssetSearch.DirectorySearchAsync(
                        configOption.BaseDir,
                        assetSearchLogger,
                        configOption.IgnoreDirectoryNames,
                        task1Progress);

                    Dictionary<int, string?> directoryMap = new();
                    for (int i = 1; i <= directories.Count; ++i)
                    {
                        directoryMap.Add(i, directories[i - 1]);
                    }
                    
                    ConcurrentBag<UnityMetaFileInfo> fileList = await AssetSearch.MakeMetaListAsync(
                        directoryMap,
                        assetSearchLogger,
                        task2Progress);
                    
                    await AssetSearch.MetaYamlAsync(
                        directoryMap,
                        fileList,
                        assetSearchLogger,
                        task3Progress,
                        cancellationTokenSource.Token);
                    
                    await AssetSearch.YamlAnalyzeAsync(
                        directoryMap,
                        fileList,
                        configOption.FileExtAnalyze,
                        configOption.IgnoreGuids,
                        assetSearchLogger,
                        task4Progress,
                        cancellationTokenSource.Token);
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            
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
