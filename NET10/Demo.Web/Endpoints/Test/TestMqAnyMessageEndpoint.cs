using System.Diagnostics;
using Demo.Application.Services;
using Demo.Domain;
using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

class TestMqAnyMessageSummary : EndpointSummary
{
    public TestMqAnyMessageSummary()
    {
        Summary = "MQ Any Message Test";
        Description = "MQ Publish Any message";
        ExampleRequest = new MqPublishRequest()
        {
            Message = "Any message"
        };
        Responses[200] = "success";
        Responses[403] = "forbidden";
    }
}

public class TestMqAnyMessageEndpoint : Endpoint<MqPublishRequest>
{
    private readonly ILogger<TestMqAnyMessageEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IMqPublishService _mqPublishService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestLoggingEndpoint"/> class with the specified logger and telemetry service.
    /// </summary>
    public TestMqAnyMessageEndpoint(
        ILogger<TestMqAnyMessageEndpoint> logger,
        ITelemetryService telemetryService,
        IMqPublishService mqPublishService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _mqPublishService = mqPublishService;
    }

    public override void Configure()
    {
        Post("/api/test/mqany");
        AllowAnonymous();
        Group<MqTest>();
        Summary(new TestMqAnyMessageSummary());
    }

    /// <summary>
    /// Handles the HTTP GET request for the logging test endpoint, performing logging and telemetry operations, simulating nested activities and error handling, and returning trace information in the response.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    public override async Task HandleAsync(MqPublishRequest msg, CancellationToken ct)
    {
        try
        {
            _telemetryService.StartActivity(nameof(TestMqAnyMessageEndpoint));
            await _mqPublishService.PublishAnyAsync(msg.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(TestMqAnyMessageEndpoint));
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

