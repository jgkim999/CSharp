using FastEndpoints;

using GamePulse.Processors;
using GamePulse.Services;
using GamePulse.Sod.Services;

using OpenTelemetry.Trace;

namespace GamePulse.Sod.Endpoints.Rtt;

/// <summary>
/// 
/// </summary>
public class RttEndpointV1 : Endpoint<RttRequest>
{
    private readonly ISodBackgroundTaskQueue _taskQueue;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="tracer"></param>
    /// <param name="taskQueue"></param>
    public RttEndpointV1(ILogger<RttEndpointV1> logger, Tracer tracer, ISodBackgroundTaskQueue taskQueue)
    {
        _taskQueue = taskQueue;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void Configure()
    {
        Version(1);
        Post("/api/sod/rtt");
        AllowAnonymous();
        PreProcessor<ValidationErrorLogger<RttRequest>>();
        Throttle(
            hitLimit: 60,
            durationSeconds: 60);
        Summary(s =>
        {
            s.Summary = "Rtt 저장";
            s.Description = "Mirror 에서 측정한 rtt 값을 기록합니다.";
            s.Response(200, "Success");
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    public override async Task HandleAsync(RttRequest req, CancellationToken ct)
    {
        using var span = GamePulseActivitySource.StartActivity(nameof(RttEndpointV1));
        string? clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (clientIp is null)
        {
            await Send.StringAsync("Unknown ip address", 400, cancellation: ct);
            return;
        }
        await _taskQueue.EnqueueAsync(new RttCommand(clientIp, span));
        await Send.OkAsync("Success", ct);
    }
}
