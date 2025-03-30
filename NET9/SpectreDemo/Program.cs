using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

using Spectre.Console;

using System.Collections.Concurrent;
using System.Diagnostics;

using Unity.Tools;

namespace SpectreDemo;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var configuration = new ConfigurationBuilder()
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

            var configOption = configuration
                .GetSection("ConfigOption")
                .Get<ConfigOption>();
            Debug.Assert(configOption != null, nameof(configOption) + " == null");
            /*
            SpaceLibrary library = new SpaceLibrary(microsoftLogger);

            microsoftLogger.LogInformation("Start space library");

            library.Run().Wait();
            */
            var assetSearchLogger = new SerilogLoggerFactory(inner)
                .CreateLogger<AssetSearch>();
            List<string> directories = new List<string>();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            
            //AnsiConsole.Status()
            //    .AutoRefresh(false)
            //    .Spinner(Spinner.Known.Dots)
            //    .Start("[yellow]Search Directories[/]", (StatusContext ctx) =>
            //    {
            //        //string root = "e:\\github\\Unity\\Demo1\\Assets\\";
            //        //string root = "/Users/jgkim/Documents/gitlab/projectb4/ProjectB2/Assets";
            //        AssetSearch.DirectorySearch(configOption.BaseDir, directories, assetSearchLogger);
            //        ctx.Status = "OK";
            //    });

            //Dictionary<int, string> directoryMap = new();
            //for (int i = 1; i <= directories.Count; ++i)
            //{
            //    directoryMap.Add(i, directories[i - 1]);
            //}
            microsoftLogger.LogInformation("Directory listing creation complete.");

            ConcurrentBag<UnityMetaFileInfo> fileList = new();
            //AnsiConsole.Status()
            //    .AutoRefresh(false)
            //    .Spinner(Spinner.Known.Dots)
            //    .Start("[yellow]Make meta guid[/]", ctx =>
            //    {
            //        AssetSearch.MakeMetaList(directoryMap, fileList, assetSearchLogger);
            //    });

            //AnsiConsole.Status()
            //    .AutoRefresh(false)
            //    .Spinner(Spinner.Known.Dots)
            //    .StartAsync("[yellow]Find Meta's guid[/]", async ctx =>
            //    {
            //        SpectreProgressContext spectreProgressContext = new (ctx);
            //        await AssetSearch.MetaYamlAsync(directoryMap, fileList, assetSearchLogger, spectreProgressContext, cancellationTokenSource.Token);
            //        ctx.Status = "OK";
            //    })
            //    .ConfigureAwait(false)
            //    .GetAwaiter()
            //    .GetResult();

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

                    SpectreProgressContext task1Progress = new (task1);
                    SpectreProgressContext task2Progress = new(task2);
                    SpectreProgressContext task3Progress = new(task3);

                    await AssetSearch.DirectorySearchAsync(configOption.BaseDir, directories, assetSearchLogger, task1Progress);

                    Dictionary<int, string> directoryMap = new();
                    for (int i = 1; i <= directories.Count; ++i)
                    {
                        directoryMap.Add(i, directories[i - 1]);
                    }

                    await AssetSearch.MakeMetaListAsync(directoryMap, fileList, assetSearchLogger, task2Progress);
                    await AssetSearch.MetaYamlAsync(directoryMap, fileList, assetSearchLogger, task3Progress, cancellationTokenSource.Token);
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            foreach (var file in fileList)
            {
                //AnsiConsole.WriteLine($"{file.Filename} {file.Guid}");
            }

            microsoftLogger.LogInformation("File listing creation complete.");
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
