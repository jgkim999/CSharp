using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

using Spectre.Console;

using SpectreDemo.Command;

using System.Diagnostics;

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

            ILogger<AssetSearch> assetSearchLogger = new SerilogLoggerFactory(inner)
                .CreateLogger<AssetSearch>();

            bool menuRun = true;

            while (menuRun)
            {
                AnsiConsole.Clear();

                // Create a table
                var table = new Table();

                // Add some columns
                table.AddColumn("Section");
                table.AddColumn("Value");

                // Add rows
                table.AddRow("AssetPath", $"[green]{configOption.AssetPath}[/]");
                table.AddRow("AssetDbPath", $"[green]{configOption.AssetDbPath}[/]");
                table.AddRow("BuildCachePath", $"[green]{configOption.BuildCachePath}[/]");
                table.AddRow("BuildCacheDbPath", $"[green]{configOption.BuildCacheDbPath}[/]");
                table.AddRow("Ignore Directory", $"[green]{string.Join(" ", configOption.IgnoreDirectoryNames)}[/]");
                table.AddRow("Analyze File Ext", $"[green]{string.Join(" ", configOption.FileExtAnalyze)}[/]");
                table.AddRow("Ignore Guid", $"[green]{string.Join(" ", configOption.IgnoreGuids)}[/]");

                // Render the table to the console
                AnsiConsole.Write(table);

                string buildCacheMenu = "BuildCache DB 만들기";
                string buildCacheCompareMenu = "BuildCache DB 비교하기";
                string assetMenu = "Asset DB 만들기";
                string quitMenu = "종료";

                var selectedMenu = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("원하는 [green]메뉴를 선택하세요 [/]?")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                        .AddChoices(new[]
                        {
                            buildCacheMenu,
                            buildCacheCompareMenu,
                            assetMenu,
                            quitMenu
                        }));

                if (selectedMenu is null)
                {
                    throw new ArgumentNullException(nameof(selectedMenu));
                }

                CancellationTokenSource cancellationTokenSource = new();

                if (selectedMenu == buildCacheMenu)
                {
                    MakeBuildDbCommand cmd = new(configOption, assetSearchLogger, cancellationTokenSource);
                    cmd.ExecuteAsync()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
                else if (selectedMenu == buildCacheCompareMenu)
                {
                    MakeBuildDbCommand cmd = new(configOption, assetSearchLogger, cancellationTokenSource);
                    cmd.ExecuteAsync()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
                else if (selectedMenu == assetMenu)
                {
                    MakeAssetDbCommand cmd = new(configOption, assetSearchLogger, cancellationTokenSource);
                    cmd.ExecuteAsync()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
                else if (selectedMenu == quitMenu)
                {
                    Log.Information("종료합니다.");
                    menuRun = false;
                }
                else
                {
                    throw new ArgumentException("Invalid menu selection.");
                }
            }

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
