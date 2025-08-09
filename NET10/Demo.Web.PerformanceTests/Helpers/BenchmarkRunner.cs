using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Demo.Web.PerformanceTests.Helpers;

/// <summary>
/// 벤치마크 실행을 위한 헬퍼 클래스
/// </summary>
public static class BenchmarkRunner
{
    /// <summary>
    /// 기본 벤치마크 설정을 생성합니다
    /// </summary>
    /// <returns>벤치마크 설정</returns>
    public static IConfig CreateDefaultConfig()
    {
        return DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(HtmlExporter.Default)
            .AddExporter(CsvExporter.Default)
            .AddLogger(ConsoleLogger.Default)
            .AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(3)
                .WithIterationCount(10));
    }

    /// <summary>
    /// 빠른 테스트를 위한 벤치마크 설정을 생성합니다
    /// </summary>
    /// <returns>빠른 테스트용 벤치마크 설정</returns>
    public static IConfig CreateQuickConfig()
    {
        return DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddLogger(ConsoleLogger.Default)
            .AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(1)
                .WithIterationCount(3));
    }

    /// <summary>
    /// 상세한 분석을 위한 벤치마크 설정을 생성합니다
    /// </summary>
    /// <returns>상세 분석용 벤치마크 설정</returns>
    public static IConfig CreateDetailedConfig()
    {
        return DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddDiagnoser(ThreadingDiagnoser.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(HtmlExporter.Default)
            .AddExporter(CsvExporter.Default)
            .AddExporter(JsonExporter.Full)
            .AddLogger(ConsoleLogger.Default)
            .AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(5)
                .WithIterationCount(20));
    }
}