using Demo.Application.Services;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// 로깅 테스트를 위한 엔드포인트
/// </summary>
public class TestLoggingEndpoint : EndpointWithoutRequest
{
    private readonly ILogger<TestLoggingEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestLoggingEndpoint"/> class with the specified logger and telemetry service.
    /// <summary>
    /// Initializes a new instance of the <see cref="TestLoggingEndpoint"/> class for testing logging and telemetry integration.
    /// </summary>
    public TestLoggingEndpoint(ILogger<TestLoggingEndpoint> logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Configures the endpoint to handle GET requests at /api/test/logging for testing logging and tracing integration.
    /// </summary>
    public override void Configure()
    {
        Get("/api/test/logging");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "로깅 및 트레이싱 테스트";
            s.Description = "Serilog와 OpenTelemetry 통합 테스트를 위한 엔드포인트";
        });
    }

    /// <summary>
    /// Handles the HTTP GET request for the logging test endpoint, performing logging and telemetry operations, simulating nested activities and error handling, and returning trace information in the response.
    /// </summary>
    /// <summary>
    /// Handles a GET request to test logging and telemetry integration, generating logs at multiple levels, simulating an error, and returning trace information in the response.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
