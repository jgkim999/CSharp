using FastEndpoints;
using GamePulse.Processors;
using OpenTelemetry.Trace;

namespace GamePulse.EndPoints.Sod.Rtt;

/// <summary>
/// 
/// </summary>
public class RttEndpointV1 : Endpoint<RttRequest>
{
    private readonly ILogger<RttEndpointV1> _logger;
    private readonly Tracer _tracer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="tracer"></param>
    public RttEndpointV1(ILogger<RttEndpointV1> logger, Tracer tracer)
    {
        _logger = logger;
        _tracer = tracer;
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

    public override async Task HandleAsync(RttRequest req, CancellationToken ct)
    {
        await Send.OkAsync("Success", ct);
    }
}
