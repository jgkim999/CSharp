using Demo.Application.Models;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Web.DTO;
using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

class MqRequestResponseProtoBufSummary : EndpointSummary
{
    public MqRequestResponseProtoBufSummary()
    {
        Summary = "MQ ProtoBuf Request-Response";
        Description = "ProtoBuf 직렬화를 사용하여 RabbitMQ 요청-응답 패턴을 테스트합니다.";
        Responses[200] = "ProtoBuf 요청-응답 테스트 성공";
        Responses[408] = "응답 타임아웃";
        Responses[500] = "서버 오류";
    }
}

/// <summary>
/// RabbitMQ ProtoBuf 요청-응답 패턴 테스트 엔드포인트
/// </summary>
public class MqRequestResponseProtoBufEndpoint : EndpointWithoutRequest<TestMqRequestResponseProtoBufResponse>
{
    private readonly IMqPublishService _mqPublishService;
    private readonly ILogger<MqRequestResponseProtoBufEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;

    public MqRequestResponseProtoBufEndpoint(
        IMqPublishService mqPublishService,
        ITelemetryService telemetryService,
        ILogger<MqRequestResponseProtoBufEndpoint> logger)
    {
        _mqPublishService = mqPublishService;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/mq/request-response-protobuf");
        AllowAnonymous();
        Group<MqTest>();
        Summary(new MqRequestResponseProtoBufSummary());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            using var span = _telemetryService.StartActivity(nameof(MqRequestResponseProtoBufEndpoint));

            var request = new ProtobufRequest()
            {
                Id = Ulid.NewUlid().ToString(),
                Message = "ProtoBuf 요청-응답 테스트",
                Timestamp = DateTime.Now,
                Data = new Dictionary<string, string>
                {
                    { "환경", Environment.MachineName },
                    { "프로세스ID", Environment.ProcessId.ToString() },
                    { "요청시간", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "직렬화", "ProtoBuf" }
                }
            };

            var target = "consumer-any-queue";

            _logger.LogInformation(
                "ProtoBuf 요청-응답 테스트 시작. 대상: {Target}, 요청ID: {RequestId}",
                target, request.Id);

            // 30초 타임아웃으로 ProtoBuf 요청-응답 실행
            var response = await _mqPublishService.PublishProtoBufAndWaitForResponseAsync<ProtobufRequest, ProtobufResponse>(
                target,
                request,
                TimeSpan.FromSeconds(30),
                ct);

            _logger.LogInformation(
                "ProtoBuf 응답 수신 완료. 요청ID: {RequestId}, 응답ID: {ResponseId}",
                request.Id, response.ResponseId);

            Response = new TestMqRequestResponseProtoBufResponse
            {
                Success = true,
                RequestData = request,
                ResponseData = response,
                Target = target,
                ProcessingTime = (DateTime.Now - request.Timestamp).TotalMilliseconds
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "ProtoBuf 요청-응답 타임아웃 발생");
            HttpContext.Response.StatusCode = 408;
            Response = new TestMqRequestResponseProtoBufResponse
            {
                Success = false,
                ErrorMessage = "ProtoBuf 응답 타임아웃이 발생했습니다. 대상 큐가 응답하지 않습니다.",
                ProcessingTime = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProtoBuf 요청-응답 테스트 중 오류 발생");
            HttpContext.Response.StatusCode = 500;
            Response = new TestMqRequestResponseProtoBufResponse
            {
                Success = false,
                ErrorMessage = $"오류 발생: {ex.Message}",
                ProcessingTime = 0
            };
        }
    }
}

