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
        // 애플리케이션 시작 시간 벤치마크
        BenchmarkRunner.Run<ApplicationStartupBenchmark>();
        
        // HTTP 요청 처리 오버헤드 벤치마크
        BenchmarkRunner.Run<HttpRequestBenchmark>();
        
        // 메모리 사용량 벤치마크
        BenchmarkRunner.Run<MemoryUsageBenchmark>();
        
        // 부하 테스트 벤치마크
        BenchmarkRunner.Run<LoadTestBenchmark>();
    }
}