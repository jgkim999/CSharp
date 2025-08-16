using Bogus;
using Demo.Application.Utils;
using FastEndpoints;

using Demo.Application.Processors;
using Demo.Infra.Services;
using Demo.Application.Services.Sod;
using OpenTelemetry.Trace;

namespace GamePulse.Sod.Endpoints.Rtt;

public class RttEndpointV1 : Endpoint<RttRequest>
{
    private readonly ISodBackgroundTaskQueue _taskQueue;
    private readonly ILogger<RttEndpointV1> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RttEndpointV1"/> class for handling RTT data submissions.
    /// </summary>
    /// <param name="logger"></param>
    /// <summary>
    /// Initializes a new instance of <see cref="RttEndpointV1"/> with the provided logger and background task queue.
    /// </summary>
    public RttEndpointV1(ILogger<RttEndpointV1> logger, ISodBackgroundTaskQueue taskQueue)
    {
        _logger = logger;
        _taskQueue = taskQueue;
    }

    /// <summary>
    /// Configures the RTT endpoint to accept anonymous HTTP POST requests for recording round-trip time values measured by Mirror.
    /// <summary>
    /// Configures the RTT v1 endpoint: registers POST /api/sod/rtt (version 1), allows anonymous access,
    /// adds request validation pre-processing, and provides documentation metadata including summary,
    /// description, response mapping, and an example request.
    /// </summary>
    /// <remarks>
    /// The endpoint accepts RttRequest payloads and is documented with a sample request (Type = "client",
    /// Rtt between 8 and 200). A throttle configuration is present in code but currently commented out.
    /// </remarks>
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
            s.ExampleRequest = new RttRequest()
            {
                Type = "client",
                Rtt = Random.Shared.Next(8, 200)
            };
        });
    }

    /// <summary>
    /// Processes an incoming RTT request by validating the client IP address and enqueuing an RTT command for background processing.
    /// </summary>
    /// <param name="req">The RTT request payload submitted by the client.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <remarks>
    /// Responds with HTTP 400 if the client IP address cannot be determined; otherwise, enqueues the RTT command and responds with HTTP 200.
    /// <summary>
    /// Handles an incoming RTT submission: validates the client IP, enqueues a background RttCommand, and returns an HTTP response.
    /// </summary>
    /// <param name="req">The incoming RTT payload.</param>
    /// <param name="ct">Cancellation token for the request lifetime.</param>
    /// <returns>A task representing the asynchronous operation; on success sends HTTP 200, on unknown IP sends HTTP 400, and on error sends an error response.</returns>
    /// <remarks>
    /// Side effects:
    /// - Enqueues an <c>RttCommand</c> into the background task queue for processing.
    /// - Sends HTTP responses directly (200 "Success", 400 "Unknown ip address", or an error payload on exception).
    /// - Logs exceptions when they occur.
    /// </remarks>
    public override async Task HandleAsync(RttRequest req, CancellationToken ct)
    {
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, nameof(RttEndpointV1));
            AddError(e.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}
