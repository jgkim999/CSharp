using System.Net;

namespace Demo.Web.PerformanceTests.Models;

/// <summary>
/// 개별 요청의 결과를 나타내는 클래스
/// </summary>
public class RequestResult
{
    public int ClientId { get; set; }
    public int RequestId { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 부하 테스트 전체 결과를 나타내는 클래스
/// </summary>
public class LoadTestResult
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int RateLimitedRequests { get; set; }
    public int FailedRequests { get; set; }
    public TimeSpan TotalTime { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public double RequestsPerSecond { get; set; }

    public override string ToString()
    {
        return $"Total: {TotalRequests}, Success: {SuccessfulRequests}, RateLimited: {RateLimitedRequests}, " +
               $"Failed: {FailedRequests}, Avg Response: {AverageResponseTime.TotalMilliseconds:F2}ms, " +
               $"RPS: {RequestsPerSecond:F2}";
    }
}

/// <summary>
/// 메모리 테스트 결과를 나타내는 클래스
/// </summary>
public class MemoryTestResult
{
    public int UniqueIpCount { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int RateLimitedRequests { get; set; }
    public long MemoryUsedBytes { get; set; }
    public double MemoryUsedMB { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public double MemoryPerIP { get; set; }
    public List<long>? MemorySnapshots { get; set; }

    public override string ToString()
    {
        return $"IPs: {UniqueIpCount}, Requests: {TotalRequests}, Success: {SuccessfulRequests}, " +
               $"RateLimited: {RateLimitedRequests}, Memory: {MemoryUsedMB:F2}MB, " +
               $"Memory/IP: {MemoryPerIP:F2}bytes, Time: {ExecutionTime.TotalSeconds:F2}s";
    }
}