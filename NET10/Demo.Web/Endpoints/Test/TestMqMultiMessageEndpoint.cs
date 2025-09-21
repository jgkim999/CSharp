using Demo.Application.Services;
using Demo.Domain;
using System.Diagnostics;
using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// 
/// </summary>
class TestMqMultiMessageSummary : EndpointSummary
{
    /// <summary>
    /// 
    /// </summary>
    public TestMqMultiMessageSummary()
    {
        Summary = "MQ Multi Message Test";
        Description = "MQ Publish Multi message";
        ExampleRequest = new MqPublishRequest()
        {
            Message = "Multi message"
        };
        Responses[200] = "success";
        Responses[403] = "forbidden";
    }
}

/// <summary>
/// 
/// </summary>
public class TestMqMultiMessageEndpoint : Endpoint<MqPublishRequest>
{
    private readonly ILogger<TestMqMultiMessageEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IMqPublishService _mqPublishService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestLoggingEndpoint"/> class with the specified logger and telemetry service.
    /// </summary>
    public TestMqMultiMessageEndpoint(
        ILogger<TestMqMultiMessageEndpoint> logger,
        ITelemetryService telemetryService,
        IMqPublishService mqPublishService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _mqPublishService = mqPublishService;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void Configure()
    {
        Post("/api/test/mqmulti");
        AllowAnonymous();
        Group<MqTest>();
        Summary(new TestMqMultiMessageSummary());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    public override async Task HandleAsync(MqPublishRequest msg, CancellationToken ct)
    {
        try
        {
            using var span = _telemetryService.StartActivity(nameof(TestMqAnyMessageEndpoint));
            await _mqPublishService.PublishMultiAsync(msg.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(TestMqAnyMessageEndpoint));
        }
        
        Response = new
        {
            Message = "MQ 테스트가 완료되었습니다.",
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
