using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Demo.Application.Extensions;

/// <summary>
/// OpenTelemetry 샘플링 전략을 구현하는 클래스
/// </summary>
public static class SamplingStrategies
{
    /// <summary>
    /// 환경별 샘플링 전략을 생성합니다.
    /// </summary>
    /// <param name="environment">환경 이름</param>
    /// <param name="defaultSamplingRatio">기본 샘플링 비율</param>
    /// <returns>구성된 샘플러</returns>
    public static Sampler CreateEnvironmentBasedSampler(string environment, double defaultSamplingRatio)
    {
        return environment.ToLowerInvariant() switch
        {
            "development" => CreateDevelopmentSampler(),
            "staging" => CreateStagingSampler(),
            "production" => CreateProductionSampler(defaultSamplingRatio),
            _ => new TraceIdRatioBasedSampler(defaultSamplingRatio)
        };
    }

    /// <summary>
    /// 개발 환경용 샘플러를 생성합니다.
    /// 모든 트레이스를 샘플링하되, 헬스체크와 정적 파일은 제외합니다.
    /// </summary>
    /// <returns>개발 환경용 샘플러</returns>
    private static Sampler CreateDevelopmentSampler()
    {
        return new ParentBasedSampler(
            new CompositeSampler(
                new TraceIdRatioBasedSampler(1.0), // 모든 트레이스 샘플링
                new HealthCheckFilterSampler()     // 헬스체크 필터링
            )
        );
    }

    /// <summary>
    /// 스테이징 환경용 샘플러를 생성합니다.
    /// 높은 샘플링 비율을 유지하면서 성능을 고려합니다.
    /// </summary>
    /// <returns>스테이징 환경용 샘플러</returns>
    private static Sampler CreateStagingSampler()
    {
        return new ParentBasedSampler(
            new CompositeSampler(
                new TraceIdRatioBasedSampler(0.5), // 50% 샘플링
                new HealthCheckFilterSampler(),
                new StaticFileFilterSampler()
            )
        );
    }

    /// <summary>
    /// 프로덕션 환경용 샘플러를 생성합니다.
    /// 성능을 최우선으로 하면서 필요한 관찰 가능성을 유지합니다.
    /// </summary>
    /// <param name="samplingRatio">기본 샘플링 비율</param>
    /// <returns>프로덕션 환경용 샘플러</returns>
    private static Sampler CreateProductionSampler(double samplingRatio)
    {
        return new ParentBasedSampler(
            new CompositeSampler(
                new AdaptiveSampler(samplingRatio), // 적응형 샘플링
                new HealthCheckFilterSampler(),
                new StaticFileFilterSampler(),
                new ErrorBasedSampler()             // 오류 기반 샘플링
            )
        );
    }
}

/// <summary>
/// 여러 샘플러를 조합하는 복합 샘플러
/// </summary>
public class CompositeSampler : Sampler
{
    private readonly Sampler _primarySampler;
    private readonly IEnumerable<Sampler> _filterSamplers;

    /// <summary>
    /// CompositeSampler 생성자
    /// </summary>
    /// <param name="primarySampler">주 샘플러</param>
    /// <param name="filterSamplers">필터 샘플러들</param>
    public CompositeSampler(Sampler primarySampler, params Sampler[] filterSamplers)
    {
        _primarySampler = primarySampler ?? throw new ArgumentNullException(nameof(primarySampler));
        _filterSamplers = filterSamplers ?? throw new ArgumentNullException(nameof(filterSamplers));
    }

    /// <summary>
    /// 샘플링 결정을 수행합니다.
    /// </summary>
    /// <param name="samplingParameters">샘플링 매개변수</param>
    /// <returns>샘플링 결과</returns>
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // 필터 샘플러들을 먼저 확인
        foreach (var filterSampler in _filterSamplers)
        {
            var filterResult = filterSampler.ShouldSample(samplingParameters);
            if (filterResult.Decision == SamplingDecision.Drop)
            {
                return filterResult;
            }
        }

        // 주 샘플러로 최종 결정
        return _primarySampler.ShouldSample(samplingParameters);
    }
}

/// <summary>
/// 헬스체크 엔드포인트를 필터링하는 샘플러
/// </summary>
public class HealthCheckFilterSampler : Sampler
{
    private static readonly HashSet<string> HealthCheckPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/ready",
        "/health/live",
        "/healthz",
        "/ping",
        "/status",
        "/metrics",
        "/favicon.ico"
    };

    /// <summary>
    /// 헬스체크 관련 요청을 필터링합니다.
    /// </summary>
    /// <param name="samplingParameters">샘플링 매개변수</param>
    /// <returns>샘플링 결과</returns>
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Activity에서 HTTP 경로 추출
        var httpTarget = Activity.Current?.GetTagItem("http.target") as string;
        var urlPath = Activity.Current?.GetTagItem("url.path") as string;
        
        var path = httpTarget ?? urlPath;
        if (!string.IsNullOrEmpty(path) && HealthCheckPaths.Contains(path))
        {
            return new SamplingResult(SamplingDecision.Drop);
        }

        // 헬스체크가 아닌 경우 샘플링 진행
        return new SamplingResult(SamplingDecision.RecordAndSample);
    }
}

/// <summary>
/// 정적 파일 요청을 필터링하는 샘플러
/// </summary>
public class StaticFileFilterSampler : Sampler
{
    private static readonly HashSet<string> StaticFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot"
    };

    /// <summary>
    /// 정적 파일 요청을 필터링합니다.
    /// </summary>
    /// <param name="samplingParameters">샘플링 매개변수</param>
    /// <returns>샘플링 결과</returns>
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Activity에서 HTTP 경로 추출
        var httpTarget = Activity.Current?.GetTagItem("http.target") as string;
        var urlPath = Activity.Current?.GetTagItem("url.path") as string;
        
        var path = httpTarget ?? urlPath;
        if (!string.IsNullOrEmpty(path))
        {
            var extension = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(extension) && StaticFileExtensions.Contains(extension))
            {
                return new SamplingResult(SamplingDecision.Drop);
            }
        }

        // 정적 파일이 아닌 경우 샘플링 진행
        return new SamplingResult(SamplingDecision.RecordAndSample);
    }
}

/// <summary>
/// 오류 기반 샘플링을 수행하는 샘플러
/// 오류가 발생한 트레이스는 항상 샘플링합니다.
/// </summary>
public class ErrorBasedSampler : Sampler
{
    /// <summary>
    /// 오류 기반 샘플링을 수행합니다.
    /// </summary>
    /// <param name="samplingParameters">샘플링 매개변수</param>
    /// <returns>샘플링 결과</returns>
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // 현재 Activity에서 오류 상태 확인
        if (Activity.Current != null)
        {
            // HTTP 상태 코드 확인
            if (Activity.Current.GetTagItem("http.response.status_code") is string statusCodeStr &&
                int.TryParse(statusCodeStr, out var statusCode) &&
                statusCode >= 400)
            {
                return new SamplingResult(SamplingDecision.RecordAndSample);
            }

            // Activity 상태 확인
            if (Activity.Current.Status == ActivityStatusCode.Error)
            {
                return new SamplingResult(SamplingDecision.RecordAndSample);
            }

            // 예외 태그 확인
            if (Activity.Current.GetTagItem("error") is string errorTag && !string.IsNullOrEmpty(errorTag))
            {
                return new SamplingResult(SamplingDecision.RecordAndSample);
            }
        }

        // 오류가 없는 경우 일반 샘플링 진행
        return new SamplingResult(SamplingDecision.RecordAndSample);
    }
}

/// <summary>
/// 적응형 샘플링을 수행하는 샘플러
/// 시스템 부하에 따라 샘플링 비율을 동적으로 조정합니다.
/// </summary>
public class AdaptiveSampler : Sampler
{
    private readonly double _baseSamplingRatio;
    private readonly Timer _adjustmentTimer;
    private double _currentSamplingRatio;
    private long _requestCount;
    private long _lastRequestCount;
    private readonly object _lock = new();

    /// <summary>
    /// AdaptiveSampler 생성자
    /// </summary>
    /// <param name="baseSamplingRatio">기본 샘플링 비율</param>
    public AdaptiveSampler(double baseSamplingRatio)
    {
        _baseSamplingRatio = Math.Clamp(baseSamplingRatio, 0.0, 1.0);
        _currentSamplingRatio = _baseSamplingRatio;
        
        // 1분마다 샘플링 비율 조정
        _adjustmentTimer = new Timer(AdjustSamplingRatio, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// 적응형 샘플링을 수행합니다.
    /// </summary>
    /// <param name="samplingParameters">샘플링 매개변수</param>
    /// <returns>샘플링 결과</returns>
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        Interlocked.Increment(ref _requestCount);

        // TraceId 기반 샘플링 (일관성 보장)
        var traceId = samplingParameters.TraceId;
        var hash = traceId.GetHashCode();
        var normalizedHash = Math.Abs(hash) / (double)int.MaxValue;
        var shouldSample = normalizedHash < _currentSamplingRatio;

        return shouldSample 
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
    }

    /// <summary>
    /// 시스템 부하에 따라 샘플링 비율을 조정합니다.
    /// </summary>
    /// <param name="state">타이머 상태</param>
    private void AdjustSamplingRatio(object? state)
    {
        lock (_lock)
        {
            var currentCount = Interlocked.Read(ref _requestCount);
            var requestsPerMinute = currentCount - _lastRequestCount;
            _lastRequestCount = currentCount;

            // 요청 수에 따른 샘플링 비율 조정
            var adjustedRatio = requestsPerMinute switch
            {
                < 100 => _baseSamplingRatio,                    // 낮은 부하: 기본 비율
                < 1000 => _baseSamplingRatio * 0.8,            // 중간 부하: 20% 감소
                < 5000 => _baseSamplingRatio * 0.5,            // 높은 부하: 50% 감소
                _ => _baseSamplingRatio * 0.1                   // 매우 높은 부하: 90% 감소
            };

            _currentSamplingRatio = Math.Clamp(adjustedRatio, 0.01, 1.0); // 최소 1% 유지
        }
    }

    /// <summary>
    /// 리소스를 해제합니다.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _adjustmentTimer?.Dispose();
        }
    }

    /// <summary>
    /// 리소스를 해제합니다.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}