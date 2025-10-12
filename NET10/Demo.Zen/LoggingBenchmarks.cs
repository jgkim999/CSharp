namespace Demo.Zen;

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[MarkdownExporter]
[MemoryDiagnoser]
//[AllStatisticsColumn]
public class LoggingBenchmarks
{
    private readonly ILogger<LoggingBenchmarks> _logger;
    private static readonly Action<ILogger, int, Exception?> _defineDelegate =
        LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(1000, "RetrieveForecast"),
            "Retrieving weather forecast for {Days} days");

    public LoggingBenchmarks()
    {
        // Use a NullLogger to avoid actual I/O
        _logger = LoggerFactory.Create(builder => builder.AddProvider(NullLoggerProvider.Instance)).CreateLogger<LoggingBenchmarks>();
    }

    [Benchmark(Baseline = true)]
    public void StringFormat()
    {
        _logger.LogDebug("Retrieving weather forecast for {0} days", 5);
    }

    [Benchmark]
    public void StringIterpolation()
    {
        _logger.LogDebug($"Retrieving weather forecast for {5} days");
    }

    // Structured logging with parameter
    [Benchmark]
    public void NaiveLogDebug()
    {
        _logger.LogDebug("Retrieving weather forecast for {Days} days", 5);
    }

    [Benchmark]
    public void IsEnabledGuard()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Retrieving weather forecast for {Days} days", 5);
        }
    }

    [Benchmark]
    public void LoggerMessageDefine()
    {
        _defineDelegate(_logger, 5, null);
    }

    [Benchmark]
    public void SourceGenerated()
    {
        LoggingExtensions.RetrieveForecast(_logger, 5);
    }
}

// Source-generated logging extension (requires Microsoft.Extensions.Logging.Generators)
public static partial class LoggingExtensions
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Retrieving weather forecast for {Days} days")]
    public static partial void RetrieveForecast(this ILogger logger, int days);
}
