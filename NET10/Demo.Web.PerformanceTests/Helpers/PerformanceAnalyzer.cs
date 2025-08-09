using System.Text.Json;

namespace Demo.Web.PerformanceTests.Helpers;

/// <summary>
/// 성능 벤치마크 결과를 분석하는 헬퍼 클래스
/// </summary>
public static class PerformanceAnalyzer
{
    /// <summary>
    /// 성능 기준을 정의하는 구조체
    /// </summary>
    public struct PerformanceCriteria
    {
        public double MaxStartupTimeIncreasePercent { get; set; }
        public double MaxRequestProcessingIncreasePercent { get; set; }
        public long MaxMemoryIncreaseBytes { get; set; }
        public double MinRequestsPerSecond { get; set; }
        public TimeSpan MaxAverageResponseTime { get; set; }
    }

    /// <summary>
    /// 기본 성능 기준을 반환합니다
    /// </summary>
    /// <returns>기본 성능 기준</returns>
    public static PerformanceCriteria GetDefaultCriteria()
    {
        return new PerformanceCriteria
        {
            MaxStartupTimeIncreasePercent = 10.0, // 시작 시간 10% 이내 증가
            MaxRequestProcessingIncreasePercent = 5.0, // 요청 처리 시간 5% 이내 증가
            MaxMemoryIncreaseBytes = 50 * 1024 * 1024, // 메모리 사용량 50MB 이내 증가
            MinRequestsPerSecond = 100, // 최소 초당 100 요청 처리
            MaxAverageResponseTime = TimeSpan.FromMilliseconds(100) // 평균 응답 시간 100ms 이내
        };
    }

    /// <summary>
    /// 시작 시간 성능을 분석합니다
    /// </summary>
    /// <param name="baselineTime">기준 시작 시간</param>
    /// <param name="openTelemetryTime">OpenTelemetry 포함 시작 시간</param>
    /// <param name="criteria">성능 기준</param>
    /// <returns>분석 결과</returns>
    public static PerformanceAnalysisResult AnalyzeStartupPerformance(
        TimeSpan baselineTime, 
        TimeSpan openTelemetryTime, 
        PerformanceCriteria criteria)
    {
        var increasePercent = ((openTelemetryTime.TotalMilliseconds - baselineTime.TotalMilliseconds) 
                              / baselineTime.TotalMilliseconds) * 100;

        var isPassing = increasePercent <= criteria.MaxStartupTimeIncreasePercent;

        return new PerformanceAnalysisResult
        {
            TestName = "애플리케이션 시작 시간",
            BaselineValue = baselineTime.TotalMilliseconds,
            TestValue = openTelemetryTime.TotalMilliseconds,
            IncreasePercent = increasePercent,
            IsPassing = isPassing,
            Threshold = criteria.MaxStartupTimeIncreasePercent,
            Unit = "ms",
            Details = $"기준: {baselineTime.TotalMilliseconds:F2}ms, " +
                     $"OpenTelemetry: {openTelemetryTime.TotalMilliseconds:F2}ms, " +
                     $"증가율: {increasePercent:F2}%"
        };
    }

    /// <summary>
    /// HTTP 요청 처리 성능을 분석합니다
    /// </summary>
    /// <param name="baselineTime">기준 처리 시간</param>
    /// <param name="openTelemetryTime">OpenTelemetry 포함 처리 시간</param>
    /// <param name="criteria">성능 기준</param>
    /// <returns>분석 결과</returns>
    public static PerformanceAnalysisResult AnalyzeRequestProcessingPerformance(
        TimeSpan baselineTime, 
        TimeSpan openTelemetryTime, 
        PerformanceCriteria criteria)
    {
        var increasePercent = ((openTelemetryTime.TotalMilliseconds - baselineTime.TotalMilliseconds) 
                              / baselineTime.TotalMilliseconds) * 100;

        var isPassing = increasePercent <= criteria.MaxRequestProcessingIncreasePercent;

        return new PerformanceAnalysisResult
        {
            TestName = "HTTP 요청 처리 시간",
            BaselineValue = baselineTime.TotalMilliseconds,
            TestValue = openTelemetryTime.TotalMilliseconds,
            IncreasePercent = increasePercent,
            IsPassing = isPassing,
            Threshold = criteria.MaxRequestProcessingIncreasePercent,
            Unit = "ms",
            Details = $"기준: {baselineTime.TotalMilliseconds:F2}ms, " +
                     $"OpenTelemetry: {openTelemetryTime.TotalMilliseconds:F2}ms, " +
                     $"증가율: {increasePercent:F2}%"
        };
    }

    /// <summary>
    /// 메모리 사용량 성능을 분석합니다
    /// </summary>
    /// <param name="baselineMemory">기준 메모리 사용량</param>
    /// <param name="openTelemetryMemory">OpenTelemetry 포함 메모리 사용량</param>
    /// <param name="criteria">성능 기준</param>
    /// <returns>분석 결과</returns>
    public static PerformanceAnalysisResult AnalyzeMemoryUsage(
        long baselineMemory, 
        long openTelemetryMemory, 
        PerformanceCriteria criteria)
    {
        var memoryIncrease = openTelemetryMemory - baselineMemory;
        var increasePercent = baselineMemory > 0 ? (memoryIncrease / (double)baselineMemory) * 100 : 0;

        var isPassing = memoryIncrease <= criteria.MaxMemoryIncreaseBytes;

        return new PerformanceAnalysisResult
        {
            TestName = "메모리 사용량",
            BaselineValue = baselineMemory / (1024.0 * 1024.0), // MB로 변환
            TestValue = openTelemetryMemory / (1024.0 * 1024.0), // MB로 변환
            IncreasePercent = increasePercent,
            IsPassing = isPassing,
            Threshold = criteria.MaxMemoryIncreaseBytes / (1024.0 * 1024.0), // MB로 변환
            Unit = "MB",
            Details = $"기준: {baselineMemory / (1024.0 * 1024.0):F2}MB, " +
                     $"OpenTelemetry: {openTelemetryMemory / (1024.0 * 1024.0):F2}MB, " +
                     $"증가량: {memoryIncrease / (1024.0 * 1024.0):F2}MB"
        };
    }

    /// <summary>
    /// 부하 테스트 성능을 분석합니다
    /// </summary>
    /// <param name="baselineRps">기준 초당 요청 수</param>
    /// <param name="openTelemetryRps">OpenTelemetry 포함 초당 요청 수</param>
    /// <param name="criteria">성능 기준</param>
    /// <returns>분석 결과</returns>
    public static PerformanceAnalysisResult AnalyzeLoadTestPerformance(
        double baselineRps, 
        double openTelemetryRps, 
        PerformanceCriteria criteria)
    {
        var decreasePercent = ((baselineRps - openTelemetryRps) / baselineRps) * 100;

        var isPassing = openTelemetryRps >= criteria.MinRequestsPerSecond && 
                       decreasePercent <= criteria.MaxRequestProcessingIncreasePercent;

        return new PerformanceAnalysisResult
        {
            TestName = "부하 테스트 (초당 요청 수)",
            BaselineValue = baselineRps,
            TestValue = openTelemetryRps,
            IncreasePercent = -decreasePercent, // 감소는 음수로 표시
            IsPassing = isPassing,
            Threshold = criteria.MinRequestsPerSecond,
            Unit = "req/s",
            Details = $"기준: {baselineRps:F2} req/s, " +
                     $"OpenTelemetry: {openTelemetryRps:F2} req/s, " +
                     $"변화율: {-decreasePercent:F2}%"
        };
    }

    /// <summary>
    /// 전체 성능 분석 결과를 생성합니다
    /// </summary>
    /// <param name="results">개별 분석 결과 목록</param>
    /// <returns>전체 성능 분석 보고서</returns>
    public static PerformanceReport GenerateReport(List<PerformanceAnalysisResult> results)
    {
        var passingTests = results.Count(r => r.IsPassing);
        var totalTests = results.Count;
        var overallPassing = passingTests == totalTests;

        return new PerformanceReport
        {
            OverallResult = overallPassing ? "통과" : "실패",
            PassingTests = passingTests,
            TotalTests = totalTests,
            PassRate = (double)passingTests / totalTests * 100,
            Results = results,
            GeneratedAt = DateTime.UtcNow,
            Summary = GenerateSummary(results)
        };
    }

    /// <summary>
    /// 성능 분석 요약을 생성합니다
    /// </summary>
    /// <param name="results">분석 결과 목록</param>
    /// <returns>요약 문자열</returns>
    private static string GenerateSummary(List<PerformanceAnalysisResult> results)
    {
        var summary = new List<string>();
        
        foreach (var result in results)
        {
            var status = result.IsPassing ? "✅" : "❌";
            summary.Add($"{status} {result.TestName}: {result.IncreasePercent:F2}% 변화");
        }

        return string.Join("\n", summary);
    }

    /// <summary>
    /// 성능 분석 결과를 JSON 파일로 저장합니다
    /// </summary>
    /// <param name="report">성능 보고서</param>
    /// <param name="filePath">저장할 파일 경로</param>
    public static async Task SaveReportToJsonAsync(PerformanceReport report, string filePath)
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
    /// 성능 분석 결과를 마크다운 파일로 저장합니다
    /// </summary>
    /// <param name="report">성능 보고서</param>
    /// <param name="filePath">저장할 파일 경로</param>
    public static async Task SaveReportToMarkdownAsync(PerformanceReport report, string filePath)
    {
        var markdown = GenerateMarkdownReport(report);
        await File.WriteAllTextAsync(filePath, markdown);
    }

    /// <summary>
    /// 마크다운 형식의 성능 보고서를 생성합니다
    /// </summary>
    /// <param name="report">성능 보고서</param>
    /// <returns>마크다운 문자열</returns>
    private static string GenerateMarkdownReport(PerformanceReport report)
    {
        var markdown = new List<string>
        {
            "# OpenTelemetry 성능 벤치마크 보고서",
            "",
            $"**생성 일시:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC",
            $"**전체 결과:** {report.OverallResult}",
            $"**통과율:** {report.PassRate:F1}% ({report.PassingTests}/{report.TotalTests})",
            "",
            "## 요약",
            "",
            report.Summary,
            "",
            "## 상세 결과",
            "",
            "| 테스트 항목 | 기준값 | 측정값 | 변화율 | 임계값 | 결과 |",
            "|------------|--------|--------|--------|--------|------|"
        };

        foreach (var result in report.Results)
        {
            var status = result.IsPassing ? "✅ 통과" : "❌ 실패";
            markdown.Add($"| {result.TestName} | {result.BaselineValue:F2} {result.Unit} | " +
                        $"{result.TestValue:F2} {result.Unit} | {result.IncreasePercent:F2}% | " +
                        $"{result.Threshold:F2} {result.Unit} | {status} |");
        }

        markdown.Add("");
        markdown.Add("## 상세 정보");
        markdown.Add("");

        foreach (var result in report.Results)
        {
            markdown.Add($"### {result.TestName}");
            markdown.Add("");
            markdown.Add(result.Details);
            markdown.Add("");
        }

        return string.Join("\n", markdown);
    }
}

/// <summary>
/// 성능 분석 결과를 담는 구조체
/// </summary>
public struct PerformanceAnalysisResult
{
    public string TestName { get; set; }
    public double BaselineValue { get; set; }
    public double TestValue { get; set; }
    public double IncreasePercent { get; set; }
    public bool IsPassing { get; set; }
    public double Threshold { get; set; }
    public string Unit { get; set; }
    public string Details { get; set; }
}

/// <summary>
/// 전체 성능 보고서를 담는 구조체
/// </summary>
public struct PerformanceReport
{
    public string OverallResult { get; set; }
    public int PassingTests { get; set; }
    public int TotalTests { get; set; }
    public double PassRate { get; set; }
    public List<PerformanceAnalysisResult> Results { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Summary { get; set; }
}