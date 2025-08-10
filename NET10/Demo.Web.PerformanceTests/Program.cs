using BenchmarkDotNet.Running;
using Demo.Web.PerformanceTests.Benchmarks;

namespace Demo.Web.PerformanceTests;

/// <summary>
/// 성능 벤치마크 테스트 프로그램 진입점
/// </summary>
public class Program
{
    /// <summary>
    /// 메인 메서드 - 모든 벤치마크 테스트를 실행합니다
    /// </summary>
    /// <param name="args">명령줄 인수</param>
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "ratelimit")
        {
            // Rate Limiting 전용 벤치마크 실행
            Console.WriteLine("=== Rate Limiting Performance Benchmarks ===");
            
            Console.WriteLine("\n1. Rate Limiting Performance Benchmark");
            BenchmarkRunner.Run<RateLimitingPerformanceBenchmark>();
            
            Console.WriteLine("\n2. Rate Limiting Load Test Benchmark");
            BenchmarkRunner.Run<RateLimitingLoadTestBenchmark>();
            
            Console.WriteLine("\n3. Rate Limiting Memory Benchmark");
            BenchmarkRunner.Run<RateLimitingMemoryBenchmark>();
        }
        else
        {
            // 기존 벤치마크 실행
            Console.WriteLine("=== General Performance Benchmarks ===");
            
            // 애플리케이션 시작 시간 벤치마크
            BenchmarkRunner.Run<ApplicationStartupBenchmark>();
            
            // HTTP 요청 처리 오버헤드 벤치마크
            BenchmarkRunner.Run<HttpRequestBenchmark>();
            
            // 메모리 사용량 벤치마크
            BenchmarkRunner.Run<MemoryUsageBenchmark>();
            
            // 부하 테스트 벤치마크
            BenchmarkRunner.Run<LoadTestBenchmark>();
            
            Console.WriteLine("\n=== Rate Limiting Performance Benchmarks ===");
            
            // Rate Limiting 벤치마크 추가
            BenchmarkRunner.Run<RateLimitingPerformanceBenchmark>();
            BenchmarkRunner.Run<RateLimitingLoadTestBenchmark>();
            BenchmarkRunner.Run<RateLimitingMemoryBenchmark>();
        }
        
        Console.WriteLine("\n=== All Benchmarks Completed ===");
        Console.WriteLine("Results are saved in BenchmarkDotNet.Artifacts folder");
    }
}