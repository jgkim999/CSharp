using FastEndpoints;

using JsonToLog.Features.LogSend;
using JsonToLog.Services;

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

using System.Diagnostics;

namespace JsonToLog.Features.Fake;

public class FakeEndpointV1 : EndpointWithoutRequest
{
    private readonly ILogger<FakeEndpointV1> _logger;
    private readonly LogSendProcessor _logSendProcessor;
    
    public FakeEndpointV1(ILogger<FakeEndpointV1> logger, LogSendProcessor logSendProcessor)
    {
        _logger = logger;
        _logSendProcessor = logSendProcessor;
    }
    
    public override void Configure()
    {
        Get("/api/fake");
        AllowAnonymous();
        Version(1);
        Description(x => x
            .WithName("FakeEndpoint V1")
            .WithSummary("A simple test endpoint")
            .Produces<object>(200)
            .Produces(500));
    }
    
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivityService.StartActivity("FakeEndpointV1.HandleAsync", ActivityKind.Producer);

        //var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        int count = Query<int>("Count");
        
        _logger.LogInformation("Received count request: {Count}", count);

        var fake = new Bogus.Faker();
        
        for (int i = 0; i < count; ++i)
        {
            // Send a log message
            await _logSendProcessor.SendLogAsync(new LogSendTask()
            {
                LogData = new Dictionary<string, object>
                {
                    { "country", fake.Address.CountryCode() },
                    { "ip", fake.Internet.IpAddress().ToString() },
                    { "ping", fake.Random.Int(1, 1000) },
                    { "version", fake.System.Semver() },
                    { "point", $"{fake.Address.Latitude()},{fake.Address.Longitude()}" },
                },
                PropagationContext = new PropagationContext(
                    activity?.Context ?? default,
                    Baggage.Current)
            });    
        }
        await SendOkAsync(new { Message = "Fake endpoint processed successfully" }, cancellation: cancellationToken);
    }
}