// See https://aka.ms/new-console-template for more information

using Serilog;
using Serilog.Core;

using Spectre.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Serilog.Extensions.Logging;

using System.IO;

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
        
        SpaceLibrary library = new SpaceLibrary(microsoftLogger);

        microsoftLogger.LogInformation("Start space library");

        library.Run().Wait();

        microsoftLogger.LogInformation("Done with the space library");
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
