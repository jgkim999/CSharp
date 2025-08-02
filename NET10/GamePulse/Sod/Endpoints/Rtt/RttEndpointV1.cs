using FastEndpoints;

using GamePulse.Processors;
using GamePulse.Services;
using GamePulse.Sod.Services;
using GamePulse.Utils;
using OpenTelemetry.Trace;

namespace GamePulse.Sod.Endpoints.Rtt;

/// <summary>
///
/// </summary>
public class RttEndpointV1 : Endpoint<RttRequest>
{
    private readonly ISodBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="tracer"></param>
    /// <summary>
    /// Initializes a new instance of the <see cref="RttEndpointV1"/> class for handling RTT data submissions.
    /// </summary>
    public RttEndpointV1(ILogger<RttEndpointV1> logger, Tracer tracer, ISodBackgroundTaskQueue taskQueue, IServiceProvider serviceProvider)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Configures the RTT endpoint to accept anonymous HTTP POST requests for recording round-trip time values measured by Mirror.
    /// </summary>
    public override void Configure()
    {
        Version(1);
        Post("/api/sod/rtt");
        AllowAnonymous();
        PreProcessor<ValidationErrorLogger<RttRequest>>();
        //Throttle(hitLimit: 60, durationSeconds: 60);
        Summary(s =>
        {
            s.Summary = "Rtt 저장";
            s.Description = "Mirror 에서 측정한 rtt 값을 기록합니다.";
            s.Response(200, "Success");
        });
    }

    /// <summary>
    /// Processes an incoming RTT request by validating the client IP address and enqueuing an RTT command for background processing.
    /// </summary>
    /// <param name="req">The RTT request payload submitted by the client.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <remarks>
    /// Responds with HTTP 400 if the client IP address cannot be determined; otherwise, enqueues the RTT command and responds with HTTP 200.
    /// </remarks>
    public override async Task HandleAsync(RttRequest req, CancellationToken ct)
    {
        using var span = GamePulseActivitySource.StartActivity(nameof(RttEndpointV1));
        //string? clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? clientIp = FakeIpGenerator.Get();
        if (clientIp is null)
        {
            await Send.StringAsync("Unknown ip address", 400, cancellation: ct);
            return;
        }

        //var cmd = new RttCommand(clientIp, req.Rtt, req.Quality, span);
        //await cmd.ExecuteAsync(_serviceProvider, ct);
        await _taskQueue.EnqueueAsync(new RttCommand(clientIp, req.Rtt, req.Quality, span));
        await Send.OkAsync("Success", ct);
    }
}
