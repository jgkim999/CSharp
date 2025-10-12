using BenchmarkDotNet.Running;
using Spectre.Console;

namespace Demo.Zen;

public static class Program
{
    public static void Main(string[] args)
    {
        // Ask for user
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("실행시킬 항목을 선택하세요")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more job)[/]")
                .AddChoices(
                    "SerializationBenchmarks",
                    "SpanMemoryBenchmarks",
                    "LoggingBenchmarks"));

        switch (selected)
        {
            case "SerializationBenchmarks":
                BenchmarkRunner.Run<SerializationBenchmarks>();
                break;
            case "SpanMemoryBenchmarks":
                BenchmarkRunner.Run<SpanMemoryBenchmarks>();
                break;
            case "LoggingBenchmarks":
                BenchmarkRunner.Run<LoggingBenchmarks>();
                break;
            default:
                AnsiConsole.MarkupLine("[red]Invalid choice[/]");
                break;
        }
    }
}
