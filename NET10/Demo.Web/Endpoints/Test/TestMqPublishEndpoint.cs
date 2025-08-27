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
    /// <summary>
    /// Initializes a new instance of <see cref="TestMqPublishEndpoint"/> with the required services.
    /// </summary>
    public TestMqPublishEndpoint(ILogger<TestMqPublishEndpoint> logger, ITelemetryService telemetryService, IMqPublishService mqPublishService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _mqPublishService = mqPublishService;
    }

    /// <summary>
    /// Configures the endpoint's route, authorization, and OpenAPI metadata.
    /// </summary>
    /// <remarks>
    /// Maps this endpoint to POST /api/test/mq, allows anonymous access, and sets the OpenAPI summary ("MQ publish")
    /// and description ("Serilog와 OpenTelemetry 통합 테스트를 위한 엔드포인트") shown in API documentation.
    /// </remarks>
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
    /// <summary>
    /// Handles an incoming MQ publish request: starts a telemetry activity, attempts to publish the provided message, and prepares the HTTP response containing a confirmation message, current trace/span identifiers (if any), and a UTC timestamp.
    /// </summary>
    /// <param name="msg">DTO containing the message to publish.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A task that represents the asynchronous handling operation.</returns>
    public override async Task HandleAsync(MqPublishRequest msg, CancellationToken ct)
    {
        // 에러 시뮬레이션
        try
        {
            _telemetryService.StartActivity(nameof(TestMqPublishEndpoint));
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

