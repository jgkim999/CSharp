using Demo.Application.Models;
using Demo.Domain;
using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// RabbitMQ MessagePack 요청-응답 패턴 테스트 엔드포인트
/// </summary>
public class TestMqRequestResponseMessagePackEndpoint : EndpointWithoutRequest<TestMqRequestResponseMessagePackResponse>
{
    private readonly IMqPublishService _mqPublishService;
    private readonly ILogger<TestMqRequestResponseMessagePackEndpoint> _logger;

    public TestMqRequestResponseMessagePackEndpoint(
        IMqPublishService mqPublishService,
        ILogger<TestMqRequestResponseMessagePackEndpoint> logger)
    {
        _mqPublishService = mqPublishService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/mq/request-response-messagepack");
        Summary(s =>
        {
            s.Summary = "RabbitMQ MessagePack 요청-응답 패턴 테스트";
            s.Description = "MessagePack 직렬화를 사용하여 RabbitMQ 요청-응답 패턴을 테스트합니다.";
            s.Responses[200] = "MessagePack 요청-응답 테스트 성공";
            s.Responses[408] = "응답 타임아웃";
            s.Responses[500] = "서버 오류";
        });
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var request = new TestRequest
            {
                Id = Ulid.NewUlid().ToString(),
                Message = "MessagePack 요청-응답 테스트",
                Timestamp = DateTime.Now,
                Data = new Dictionary<string, object>
                {
                    { "환경", Environment.MachineName },
                    { "프로세스ID", Environment.ProcessId },
                    { "요청시간", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };

            var target = "test.messagepack.reply.queue";

            _logger.LogInformation(
                "MessagePack 요청-응답 테스트 시작. 대상: {Target}, 요청ID: {RequestId}",
                target, request.Id);

            // 30초 타임아웃으로 MessagePack 요청-응답 실행
            var response = await _mqPublishService.SendAndWaitForResponseAsync<TestRequest, TestResponse>(
                target,
                request,
                TimeSpan.FromSeconds(30),
                ct);

            _logger.LogInformation(
                "MessagePack 응답 수신 완료. 요청ID: {RequestId}, 응답ID: {ResponseId}",
                request.Id, response.ResponseId);

            Response = new TestMqRequestResponseMessagePackResponse
            {
                Success = true,
                RequestData = request,
                ResponseData = response,
                Target = target,
                ProcessingTime = response.ProcessedAt - request.Timestamp
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "MessagePack 요청-응답 타임아웃 발생");
            HttpContext.Response.StatusCode = 408;
            Response = new TestMqRequestResponseMessagePackResponse
            {
                Success = false,
                ErrorMessage = "MessagePack 응답 타임아웃이 발생했습니다. 대상 큐가 응답하지 않습니다.",
                ProcessingTime = TimeSpan.Zero
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MessagePack 요청-응답 테스트 중 오류 발생");
            HttpContext.Response.StatusCode = 500;
            Response = new TestMqRequestResponseMessagePackResponse
            {
                Success = false,
                ErrorMessage = $"오류 발생: {ex.Message}",
                ProcessingTime = TimeSpan.Zero
            };
        }
    }
}

/// <summary>
/// MessagePack 요청-응답 테스트 응답 DTO
/// </summary>
public class TestMqRequestResponseMessagePackResponse
{
    /// <summary>
    /// 테스트 성공 여부
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 전송한 요청 데이터
    /// </summary>
    public TestRequest? RequestData { get; set; }

    /// <summary>
    /// 수신한 응답 데이터
    /// </summary>
    public TestResponse? ResponseData { get; set; }

    /// <summary>
    /// 대상 큐 이름
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// 처리 시간
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; set; }
}