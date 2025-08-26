using System.ComponentModel;
using System.Diagnostics;
using Demo.Application.Services;
using Demo.Domain;
using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

public class MqPublishRequest
{
    [DefaultValue("WTF MQ")]
    public string Message { get; set; } = "Hello MQ";
}

public class TestMqPublishEndpoint : Endpoint<MqPublishRequest>
{
    private readonly ILogger<TestMqPublishEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IMqPublishService _mqPublishService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestLoggingEndpoint"/> class with the specified logger and telemetry service.
    /// </summary>
    public TestMqPublishEndpoint(ILogger<TestMqPublishEndpoint> logger, ITelemetryService telemetryService, IMqPublishService mqPublishService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _mqPublishService = mqPublishService;
    }

    public override void Configure()
    {
        Post("/api/test/mq");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "MQ publish";
            s.Description = "Serilog와 OpenTelemetry 통합 테스트를 위한 엔드포인트";
        });
    }

    /// <summary>
    /// Handles the HTTP GET request for the logging test endpoint, performing logging and telemetry operations, simulating nested activities and error handling, and returning trace information in the response.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    public override async Task HandleAsync(MqPublishRequest msg, CancellationToken ct)
    {
        // 에러 시뮬레이션
        try
        {
            await _mqPublishService.PublishMessageAsync(msg.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(TestMqPublishEndpoint));
        }
        
        Response = new
        {
            Message = "MQ 테스트가 완료되었습니다. 콘솔 로그를 확인해주세요.",
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

