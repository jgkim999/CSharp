namespace Demo.Infra.Benchmarks;

/// <summary>
/// 벤치마크 테스트 실행을 위한 메인 프로그램
/// </summary>
public class Program
{
    /// <summary>
    /// 프로그램 진입점
    /// </summary>
    /// <param name="args">명령줄 인수</param>
    public static void Main(string[] args)
    {
        Console.WriteLine("=== FusionCache 성능 벤치마크 테스트 ===");
        Console.WriteLine();
        
        if (args.Length == 0)
        {
            ShowMenu();
            return;
        }
        
        var command = args[0].ToLowerInvariant();
        
        switch (command)
        {
            case "response":
            case "responsetime":
                BenchmarkRunner.RunResponseTimeBenchmark();
                break;
                
            case "concurrency":
            case "concurrent":
                BenchmarkRunner.RunConcurrencyBenchmark();
                break;
                
            case "all":
                BenchmarkRunner.RunAllBenchmarks();
                break;
                
            default:
                Console.WriteLine($"알 수 없는 명령어: {command}");
                ShowMenu();
                break;
        }
    }
    
    /// <summary>
    /// 사용 가능한 명령어 메뉴를 표시합니다
    /// </summary>
    private static void ShowMenu()
    {
        Console.WriteLine("사용법:");
        Console.WriteLine("  dotnet run -- <command>");
        Console.WriteLine();
        Console.WriteLine("사용 가능한 명령어:");
        Console.WriteLine("  response     - 응답 시간 벤치마크 실행");
        Console.WriteLine("  concurrency  - 동시성 및 처리량 벤치마크 실행");
        Console.WriteLine("  all          - 모든 벤치마크 실행");
        Console.WriteLine();
        Console.WriteLine("예시:");
        Console.WriteLine("  dotnet run -- response");
        Console.WriteLine("  dotnet run -- concurrency");
        Console.WriteLine("  dotnet run -- all");
    }
}