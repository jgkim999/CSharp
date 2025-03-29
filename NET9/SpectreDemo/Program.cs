// See https://aka.ms/new-console-template for more information

using Serilog;
using Serilog.Core;

using Spectre.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Serilog.Extensions.Logging;

using System.IO;

using Unity.Tools;
using System.Collections.Concurrent;

namespace SpectreDemo;

class Program
{
    static void Main(string[] args)
    {
        /*
        Console.WriteLine("Hello, World!");
        AnsiConsole.Markup("[underline red]Hello[/] World!");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold yellow]Hello[/] [red]World![/]"));
        AnsiConsole.WriteLine();
        var table = new Table();
        table.AddColumn(new TableColumn(new Markup("[yellow]Foo[/]")));
        table.AddColumn(new TableColumn("[blue]Bar[/]"));
        AnsiConsole.Write(table);
        */
        Logger inner = ConfigureLogger();
        var microsoftLogger = new SerilogLoggerFactory(inner)
            .CreateLogger<SpaceLibrary>();
        
        /*
        SpaceLibrary library = new SpaceLibrary(microsoftLogger);

        microsoftLogger.LogInformation("Start space library");

        library.Run().Wait();
        */
        var assetSearchLogger = new SerilogLoggerFactory(inner)
            .CreateLogger<AssetSearch>();
        List<string> directories = new List<string>();
        
        AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Dots)
            .Start("[yellow]Search Directories[/]", (StatusContext ctx) =>
            {
                //string root = "e:\\github\\Unity\\Demo1\\Assets\\";
                string root = "/Users/jgkim/Documents/gitlab/projectb4/ProjectB2/Assets";
                AssetSearch.DirectorySearch(root, directories, assetSearchLogger);
                ctx.Status = "OK";
            });

        Dictionary<int, string> directoryMap = new ();
        for (int i = 1; i <= directories.Count; ++i)
        {
            directoryMap.Add(i, directories[i - 1]);
        }
        microsoftLogger.LogInformation("Directory listing creation complete.");

        ConcurrentBag<(int dirNum, string filanme)> fileList = new();
        AnsiConsole.Progress()
            .AutoRefresh(true)
            .Start(ctx =>
            {
                AssetSearch.MakeMetaList(directoryMap, fileList, assetSearchLogger);
            });
        /*
        for (int i=0; i<fileList.Count; ++i)
        {
            var (dirNum, filename) = fileList.ElementAt(i);
            AnsiConsole.Write(new Rows(
                new Text($"{dirNum} {directoryMap[dirNum]} {filename}")
            ));
        }
        */

        microsoftLogger.LogInformation("File listing creation complete.");
    }

    private static Logger ConfigureLogger()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        return new LoggerConfiguration()
            .ReadFrom
            .Configuration(configuration)
            .CreateLogger();
        
        /*return new LoggerConfiguration()
            .WriteTo
            .Spectre()
            .MinimumLevel.Verbose()
            .CreateLogger();*/
    }
}
