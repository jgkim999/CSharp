using System.Diagnostics;
using System.Text.Json;
using Demo.Web.PerformanceTests.Models;

namespace Demo.Web.PerformanceTests.Helpers;

/// <summary>
/// Rate Limiting 성능 분석을 위한 헬퍼 클래스
/// </summary>
public class RateLimitingPerformanceAnalyzer
{
    /// <summary>
    /// 성능 테스트 결과를 분석하고 리포트를 생성합니다.
    /// </summary>
    /// <param name="results">분석할 결과 목록</param>
    /// <returns>분석 리포트</returns>
    public static PerformanceAnalysisReport AnalyzeResults(IEnumerable<LoadTestResult> results)
    {
        var resultsList = results.ToList();
        if (!resultsList.Any())
        {
            throw new ArgumentException("분석할 결과가 없습니다.", nameof(results));
        }

        var report = new PerformanceAnalysisReport
        {
            TotalTests = resultsList.Count,
            TotalRequests = resultsList.Sum(r => r.TotalRequests),
            TotalSuccessfulRequests = resultsList.Sum(r => r.SuccessfulRequests),
            TotalRateLimitedRequests = resultsList.Sum(r => r.RateLimitedRequests),
            TotalFailedRequests = resultsList.Sum(r => r.FailedRequests),
            AverageResponseTime = TimeSpan.FromMilliseconds(
                resultsList.Average(r => r.AverageResponseTime.TotalMilliseconds)),
            MaxResponseTime = resultsList.Max(r => r.MaxResponseTime),
            MinResponseTime = resultsList.Min(r => r.MinResponseTime),
            AverageRequestsPerSecond = resultsList.Average(r => r.RequestsPerSecond),
            MaxRequestsPerSecond = resultsList.Max(r => r.RequestsPerSecond),
            MinRequestsPerSecond = resultsList.Min(r => r.RequestsPerSecond)
        };

        // 성공률 계산
        report.SuccessRate = report.TotalRequests > 0 
            ? (double)report.TotalSuccessfulRequests / report.TotalRequests * 100 
            : 0;

        // Rate Limit 적용률 계산
        report.RateLimitRate = report.TotalRequests > 0 
            ? (double)report.TotalRateLimitedRequests / report.TotalRequests * 100 
            : 0;

        // 실패율 계산
        report.FailureRate = report.TotalRequests > 0 
            ? (double)report.TotalFailedRequests / report.TotalRequests * 100 
            : 0;

        return report;
    }

    /// <summary>
    /// 메모리 사용량 결과를 분석합니다.
    /// </summary>
    /// <param name="results">분석할 메모리 테스트 결과 목록</param>
    /// <returns>메모리 분석 리포트</returns>
    public static MemoryAnalysisReport AnalyzeMemoryResults(IEnumerable<MemoryTestResult> results)
    {
        var resultsList = results.ToList();
        if (!resultsList.Any())
        {
            throw new ArgumentException("분석할 메모리 결과가 없습니다.", nameof(results));
        }

        var report = new MemoryAnalysisReport
        {
            TotalTests = resultsList.Count,
            TotalUniqueIPs = resultsList.Sum(r => r.UniqueIpCount),
            TotalRequests = resultsList.Sum(r => r.TotalRequests),
            TotalMemoryUsedMB = resultsList.Sum(r => r.MemoryUsedMB),
            AverageMemoryUsedMB = resultsList.Average(r => r.MemoryUsedMB),
            MaxMemoryUsedMB = resultsList.Max(r => r.MemoryUsedMB),
            MinMemoryUsedMB = resultsList.Min(r => r.MemoryUsedMB),
            AverageMemoryPerIP = resultsList.Average(r => r.MemoryPerIP),
            MaxMemoryPerIP = resultsList.Max(r => r.MemoryPerIP),
            MinMemoryPerIP = resultsList.Min(r => r.MemoryPerIP)
        };

        // IP당 평균 메모리 사용량 계산
        report.MemoryEfficiency = report.TotalUniqueIPs > 0 
            ? report.TotalMemoryUsedMB / report.TotalUniqueIPs 
            : 0;

        return report;
    }

    /// <summary>
    /// 성능 테스트 결과를 JSON 파일로 저장합니다.
    /// </summary>
    /// <param name="report">저장할 리포트</param>
    /// <param name="filePath">저장할 파일 경로</param>
    public static async Task SaveReportToFileAsync(object report, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// 성능 테스트 결과를 콘솔에 출력합니다.
    /// </summary>
    /// <param name="report">출력할 리포트</param>
    public static void PrintReport(PerformanceAnalysisReport report)
    {
        Console.WriteLine("=== Rate Limiting 성능 분석 리포트 ===");
        Console.WriteLine($"총 테스트 수: {report.TotalTests}");
        Console.WriteLine($"총 요청 수: {report.TotalRequests:N0}");
        Console.WriteLine($"성공한 요청 수: {report.TotalSuccessfulRequests:N0} ({report.SuccessRate:F1}%)");
        Console.WriteLine($"Rate Limit된 요청 수: {report.TotalRateLimitedRequests:N0} ({report.RateLimitRate:F1}%)");
        Console.WriteLine($"실패한 요청 수: {report.TotalFailedRequests:N0} ({report.FailureRate:F1}%)");
        Console.WriteLine();
        Console.WriteLine("=== 응답 시간 분석 ===");
        Console.WriteLine($"평균 응답 시간: {report.AverageResponseTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"최대 응답 시간: {report.MaxResponseTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"최소 응답 시간: {report.MinResponseTime.TotalMilliseconds:F2}ms");
        Console.WriteLine();
        Console.WriteLine("=== 처리량 분석 ===");
        Console.WriteLine($"평균 초당 요청 수: {report.AverageRequestsPerSecond:F2}");
        Console.WriteLine($"최대 초당 요청 수: {report.MaxRequestsPerSecond:F2}");
        Console.WriteLine($"최소 초당 요청 수: {report.MinRequestsPerSecond:F2}");
        Console.WriteLine();
    }

    /// <summary>
    /// 메모리 분석 결과를 콘솔에 출력합니다.
    /// </summary>
    /// <param name="report">출력할 메모리 리포트</param>
    public static void PrintMemoryReport(MemoryAnalysisReport report)
    {
        Console.WriteLine("=== Rate Limiting 메모리 분석 리포트 ===");
        Console.WriteLine($"총 테스트 수: {report.TotalTests}");
        Console.WriteLine($"총 고유 IP 수: {report.TotalUniqueIPs:N0}");
        Console.WriteLine($"총 요청 수: {report.TotalRequests:N0}");
        Console.WriteLine();
        Console.WriteLine("=== 메모리 사용량 분석 ===");
        Console.WriteLine($"총 메모리 사용량: {report.TotalMemoryUsedMB:F2}MB");
        Console.WriteLine($"평균 메모리 사용량: {report.AverageMemoryUsedMB:F2}MB");
        Console.WriteLine($"최대 메모리 사용량: {report.MaxMemoryUsedMB:F2}MB");
        Console.WriteLine($"최소 메모리 사용량: {report.MinMemoryUsedMB:F2}MB");
        Console.WriteLine();
        Console.WriteLine("=== IP당 메모리 사용량 분석 ===");
        Console.WriteLine($"평균 IP당 메모리: {report.AverageMemoryPerIP:F2}bytes");
        Console.WriteLine($"최대 IP당 메모리: {report.MaxMemoryPerIP:F2}bytes");
        Console.WriteLine($"최소 IP당 메모리: {report.MinMemoryPerIP:F2}bytes");
        Console.WriteLine($"메모리 효율성: {report.MemoryEfficiency:F2}MB/IP");
        Console.WriteLine();
    }

    /// <summary>
    /// 성능 벤치마크를 실행하고 결과를 분석합니다.
    /// </summary>
    /// <param name="testName">테스트 이름</param>
    /// <param name="testAction">실행할 테스트 액션</param>
    /// <returns>실행 시간과 결과</returns>
    public static async Task<(TimeSpan Duration, T Result)> RunBenchmarkAsync<T>(string testName, Func<Task<T>> testAction)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {testName} 시작...");
        
        var stopwatch = Stopwatch.StartNew();
        var result = await testAction();
        stopwatch.Stop();
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {testName} 완료 - 소요 시간: {stopwatch.Elapsed.TotalSeconds:F2}초");
        
        return (stopwatch.Elapsed, result);
    }
}

/// <summary>
/// 성능 분석 리포트 클래스
/// </summary>
public class PerformanceAnalysisReport
{
    public int TotalTests { get; set; }
    public int TotalRequests { get; set; }
    public int TotalSuccessfulRequests { get; set; }
    public int TotalRateLimitedRequests { get; set; }
    public int TotalFailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public double RateLimitRate { get; set; }
    public double FailureRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public double AverageRequestsPerSecond { get; set; }
    public double MaxRequestsPerSecond { get; set; }
    public double MinRequestsPerSecond { get; set; }
}

/// <summary>
/// 메모리 분석 리포트 클래스
/// </summary>
public class MemoryAnalysisReport
{
    public int TotalTests { get; set; }
    public int TotalUniqueIPs { get; set; }
    public int TotalRequests { get; set; }
    public double TotalMemoryUsedMB { get; set; }
    public double AverageMemoryUsedMB { get; set; }
    public double MaxMemoryUsedMB { get; set; }
    public double MinMemoryUsedMB { get; set; }
    public double AverageMemoryPerIP { get; set; }
    public double MaxMemoryPerIP { get; set; }
    public double MinMemoryPerIP { get; set; }
    public double MemoryEfficiency { get; set; }
}