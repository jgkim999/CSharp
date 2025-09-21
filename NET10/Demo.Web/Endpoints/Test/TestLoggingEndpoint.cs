using Demo.Application.Services;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// 로깅 테스트 엔드포인트의 Swagger 문서화를 위한 요약 클래스
/// FastEndpoints의 EndpointSummary를 상속받아 API 문서를 정의합니다
/// </summary>
public class TestLoggingSummary : EndpointSummary
{
    /// <summary>
    /// TestLoggingSummary의 새 인스턴스를 초기화하고 Swagger 문서 정보를 설정합니다
    /// 로깅 및 OpenTelemetry 추적 기능을 테스트하는 API입니다
    /// </summary>
    public TestLoggingSummary()
    {
        Summary = "테스트용 로깅 API입니다.";
        Description = "테스트용 로깅 API입니다.";
    }
}

/// <summary>
/// 로깅 및 OpenTelemetry 추적 기능을 테스트하는 엔드포인트 클래스
/// FastEndpoints를 사용하여 구현되며, 다양한 로깅 레벨과 중첩된 Activity를 테스트합니다
/// 예외 처리 및 오류 로깅 시리어이션도 포함합니다
/// </summary>
public class TestLoggingEndpoint : EndpointWithoutRequest
{
    private readonly ILogger<TestLoggingEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// TestLoggingEndpoint의 새 인스턴스를 초기화합니다
    /// 로거와 텔레메트리 서비스를 주입받아 로깅 및 추적 기능을 제공합니다
    /// </summary>
    /// <param name="logger">로깅을 위한 ILogger 인스턴스</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService 인스턴스</param>
    public TestLoggingEndpoint(ILogger<TestLoggingEndpoint> logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// 엔드포인트의 라우팅 및 보안 설정을 구성합니다
    /// GET /api/test/logging 경로로 익명 접근을 허용하며 LoggingTest 그룹에 속합니다
    /// </summary>
    public override void Configure()
    {
        Get("/api/test/logging");
        AllowAnonymous();
        Group<LoggingTest>();
        Summary(new TestLoggingSummary());
    }

    /// <summary>
    /// 로깅 테스트 요청을 비동기적으로 처리합니다
    /// 다양한 로깅 레벨, 중첩된 OpenTelemetry Activity, 예외 처리를 테스트하며
    /// 추적 정보와 함께 처리 결과를 반환합니다
    /// </summary>
    /// <param name="ct">작업 취소를 위한 CancellationToken</param>
    /// <returns>로깅 테스트 결과와 추적 정보가 포함된 응답</returns>
    public override async Task HandleAsync(CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("TestLogging", new Dictionary<string, object?>
        {
            ["test.type"] = "logging",
            ["test.timestamp"] = DateTimeOffset.UtcNow.ToString()
        });

        _logger.LogInformation("테스트 로그 메시지 - 정보 레벨");
        _logger.LogWarning("테스트 로그 메시지 - 경고 레벨");
        
        // TelemetryService의 헬퍼 메서드 사용
        _telemetryService.LogInformationWithTrace(_logger, "TelemetryService를 통한 로그 메시지: {TestValue}", "test-value-123");

        // 중첩된 Activity 테스트
        using var nestedActivity = _telemetryService.StartActivity("NestedOperation", new Dictionary<string, object?>
        {
            ["nested.operation"] = "database_query",
            ["nested.table"] = "users"
        });

        _logger.LogInformation("중첩된 Activity에서의 로그 메시지");
        
        // 에러 시뮬레이션
        try
        {
            throw new InvalidOperationException("테스트용 예외입니다");
        }
        catch (Exception ex)
        {
            _telemetryService.LogErrorWithTrace(_logger, ex, "예외 발생 테스트: {ErrorType}", ex.GetType().Name);
            _telemetryService.SetActivityError(nestedActivity, ex);
        }

        _telemetryService.SetActivitySuccess(activity, "로깅 테스트 완료");

        Response = new
        {
            Message = "로깅 테스트가 완료되었습니다. 콘솔 로그를 확인해주세요.",
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
        await Task.CompletedTask;
    }
}
