using BenchmarkDotNet.Running;
using System.Reflection;

namespace Demo.Infra.Tests.Benchmarks;

/// <summary>
/// 벤치마크 테스트 실행을 위한 러너 클래스
/// </summary>
public static class BenchmarkRunner
{
    /// <summary>
    /// 응답 시간 벤치마크를 실행합니다
    /// </summary>
    public static void RunResponseTimeBenchmark()
    {
        Console.WriteLine("=== IP 국가 코드 캐시 응답 시간 벤치마크 시작 ===");
        Console.WriteLine();
        
        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<IpToNationCacheResponseTimeBenchmark>();
        
        Console.WriteLine();
        Console.WriteLine("=== 벤치마크 완료 ===");
        Console.WriteLine($"결과 파일 위치: {summary.ResultsDirectoryPath}");
    }

    /// <summary>
    /// 동시성 및 처리량 벤치마크를 실행합니다
    /// </summary>
    public static void RunConcurrencyBenchmark()
    {
        Console.WriteLine("=== IP 국가 코드 캐시 동시성 벤치마크 시작 ===");
        Console.WriteLine();
        
        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<IpToNationCacheConcurrencyBenchmark>();
        
        Console.WriteLine();
        Console.WriteLine("=== 벤치마크 완료 ===");
        Console.WriteLine($"결과 파일 위치: {summary.ResultsDirectoryPath}");
    }

    /// <summary>
    /// 모든 벤치마크를 실행합니다
    /// </summary>
    public static void RunAllBenchmarks()
    {
        Console.WriteLine("=== 모든 IP 국가 코드 캐시 벤치마크 시작 ===");
        Console.WriteLine();
        
        var assembly = Assembly.GetExecutingAssembly();
        var benchmarkTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "Demo.Infra.Tests.Benchmarks" && 
                       t.Name.EndsWith("Benchmark") && 
                       !t.IsAbstract)
            .ToArray();
        
        foreach (var benchmarkType in benchmarkTypes)
        {
            Console.WriteLine($"실행 중: {benchmarkType.Name}");
            var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run(benchmarkType);
            Console.WriteLine($"완료: {benchmarkType.Name}");
            Console.WriteLine();
        }
        
        Console.WriteLine("=== 모든 벤치마크 완료 ===");
    }
}