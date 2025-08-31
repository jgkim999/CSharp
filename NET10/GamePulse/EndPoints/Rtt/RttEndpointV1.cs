using System.Diagnostics;
using Demo.Application.Processors;
using Demo.Application.Services;
using Demo.Application.Services.Sod;
using Demo.Application.Utils;
using FastEndpoints;

namespace GamePulse.EndPoints.Rtt;

// Endpoint Summary (optional but recommended)
public class RttEndpointV1Summary : Summary<RttEndpointV1>
{
    public RttEndpointV1Summary()
    {
        Summary = "Summary Summary";
        Description = "Description Description";
        Response(400, "The request is invalid (e.g., missing fields).");
        Response(409, "A user with this email already exists.");

        ExampleRequest = new RttRequest()
        {
            Type = "client",
            Rtt = Random.Shared.Next(8, 200),
            Quality = Random.Shared.Next(0, 4)
        };
    }
}

public class RttEndpointV1 : Endpoint<RttRequest>
{
    private readonly ISodBackgroundTaskQueue _taskQueue;
    private readonly ILogger<RttEndpointV1> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RttEndpointV1"/> class for handling RTT data submissions.
    /// </summary>
    /// <param name="logger">로거</param>
    /// <param name="taskQueue">백그라운드 작업 큐</param>
    /// <param name="telemetryService">텔레메트리 서비스</param>
    public RttEndpointV1(ILogger<RttEndpointV1> logger, ISodBackgroundTaskQueue taskQueue, ITelemetryService telemetryService)
    {
        _logger = logger;
        _taskQueue = taskQueue;
        _telemetryService = telemetryService;
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

        Description(b => b.WithTags("sod")
            .Accepts<RttRequest>("application/json")
            .Produces<string>(200)
            .ProducesProblemFE(400) // shortcut for .Produces<ErrorResponse>(400)
            .ProducesProblemFE<InternalErrorResponse>(500)
            .WithDescription("RTT 데이터 제출 엔드포인트"));
    }

    /// <summary>
    /// RTT 요청을 처리하여 클라이언트 IP 주소를 검증하고 백그라운드 처리를 위해 RTT 명령을 큐에 추가합니다.
    /// </summary>
    /// <param name="req">클라이언트가 제출한 RTT 요청 페이로드</param>
    /// <param name="ct">비동기 작업을 위한 취소 토큰</param>
    /// <remarks>
    /// 클라이언트 IP 주소를 확인할 수 없는 경우 HTTP 400으로 응답하고, 그렇지 않으면 RTT 명령을 큐에 추가하고 HTTP 200으로 응답합니다.
    /// </remarks>
    public override async Task HandleAsync(RttRequest req, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // OpenTelemetry Activity 시작
        using var activity = _telemetryService?.StartActivity("rtt.submit", new Dictionary<string, object?>
        {
            ["rtt.type"] = req.Type,
            ["rtt.value"] = req.Rtt,
            ["rtt.quality"] = req.Quality,
            ["endpoint.name"] = "RttEndpointV1",
            ["endpoint.version"] = "v1"
        });

        try
        {
            _telemetryService?.LogInformationWithTrace(_logger,
                "RTT 데이터 수신: Type={Type}, Rtt={Rtt}, Quality={Quality}",
                req.Type, req.Rtt, req.Quality);

            _logger.LogInformation("{Type} {Rtt} {Quality}", req.Type, req.Rtt, req.Quality);

            // 클라이언트 IP 주소 확인 (실제 환경에서는 HttpContext.Connection.RemoteIpAddress 사용)
            string? clientIp = FakeIpGenerator.Get();

            if (clientIp is null)
            {
                // IP 주소 확인 실패 메트릭 기록
                stopwatch.Stop();
                var duration = stopwatch.Elapsed.TotalSeconds;

                _telemetryService?.RecordHttpRequest("POST", "/api/sod/rtt", 400, duration);
                _telemetryService?.RecordError("ip_address_unknown", "rtt.submit", "Client IP address could not be determined");

                activity?.SetTag("error.type", "ip_address_unknown");
                activity?.SetTag("http.status_code", 400);

                _telemetryService?.LogWarningWithTrace(_logger, "클라이언트 IP 주소를 확인할 수 없습니다");

                await Send.StringAsync("Unknown ip address", 400, cancellation: ct);
                return;
            }

            // Activity에 IP 주소 추가
            activity?.SetTag("client.ip", clientIp);

            // RTT 명령을 백그라운드 큐에 추가
            await _taskQueue.EnqueueAsync(new RttCommand(clientIp, req.Rtt, req.Quality, activity));

            // 성공 메트릭 기록
            stopwatch.Stop();
            var successDuration = stopwatch.Elapsed.TotalSeconds;

            _telemetryService?.RecordHttpRequest("POST", "/api/sod/rtt", 200, successDuration);
            _telemetryService?.RecordBusinessMetric("rtt_submissions", 1, new Dictionary<string, object?>
            {
                ["rtt.type"] = req.Type,
                ["client.ip"] = clientIp
            });

            // RTT 메트릭 직접 기록 (국가 코드는 IP에서 추출해야 하지만 여기서는 예시로 "KR" 사용)
            _telemetryService?.RecordRttMetrics("KR", req.Rtt / 1000.0, req.Quality, "sod");

            activity?.SetTag("http.status_code", 200);
            activity?.SetTag("queue.enqueued", true);

            _telemetryService?.SetActivitySuccess(activity, "RTT data successfully queued for processing");

            _telemetryService?.LogInformationWithTrace(_logger,
                "RTT 데이터 처리 완료: ClientIP={ClientIP}, Duration={Duration}ms",
                clientIp, stopwatch.ElapsedMilliseconds);

            await Send.OkAsync("Success", ct);
        }
        catch (Exception ex)
        {
            // 예외 처리 및 메트릭 기록
            stopwatch.Stop();
            var errorDuration = stopwatch.Elapsed.TotalSeconds;

            _telemetryService?.RecordHttpRequest("POST", "/api/sod/rtt", 500, errorDuration);
            _telemetryService?.RecordError("rtt_processing_exception", "rtt.submit", ex.Message);
            _telemetryService?.SetActivityError(activity, ex);

            _telemetryService?.LogErrorWithTrace(_logger, ex,
                "RTT 데이터 처리 중 예외 발생: Type={Type}, Rtt={Rtt}", req.Type, req.Rtt);

            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}
