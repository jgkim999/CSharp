using FastEndpoints;

using JsonToLog.Services;
using JsonToLog.Utils;

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

using System.Diagnostics;

namespace JsonToLog.Features.LogSend;

public class LogEndpointV1 : Endpoint<LogRequest>
{
    private readonly ILogger<LogEndpointV1> _logger;
    private readonly LogSendProcessor _logSendProcessor;
    
    public LogEndpointV1(ILogger<LogEndpointV1> logger, LogSendProcessor logSendProcessor)
    {
        _logger = logger;
        _logSendProcessor = logSendProcessor;
    }
    
    public override void Configure()
    {
        Post("/api/log");
        AllowAnonymous();
        Version(1);
        Description(x => x
            .WithName("LogEndpoint V1")
            .WithSummary("A simple test endpoint")
            .Produces<object>(200)
            .Produces(500));
    }

    public override async Task HandleAsync(LogRequest request, CancellationToken cancellationToken)
    {
        using var activity = ActivityService.StartActivity("LogEndpointV1.HandleAsync", ActivityKind.Producer);
        
        var result = JsonProcessor.ExtractKeyValues(request.Payload);
        if (result.IsFailed)
        {
            await SendStringAsync(
                string.Join(", ", result.Errors.Select(e => e.Message)),
                statusCode: StatusCodes.Status400BadRequest,
                cancellation: cancellationToken);
            return;
        }
        // Log the extracted key-value pairs
        var dictionary = result.Value;
        _logger.LogInformation("Received log data: {@LogData}", dictionary);

        await _logSendProcessor.SendLogAsync(new LogSendTask()
        {
            LogData = dictionary,
            PropagationContext = new PropagationContext(
                activity?.Context ?? default,
                Baggage.Current)
        });

        await SendOkAsync(result.Value, cancellationToken);
    }
}
