using Demo.Domain;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// RabbitMQ 요청-응답 패턴 테스트 엔드포인트
/// </summary>
public class TestMqRequestResponseEndpoint : EndpointWithoutRequest<TestMqRequestResponseResponse>
{
    private readonly IMqPublishService _mqPublishService;
    private readonly ILogger<TestMqRequestResponseEndpoint> _logger;

    public TestMqRequestResponseEndpoint(
        IMqPublishService mqPublishService,
        ILogger<TestMqRequestResponseEndpoint> logger)
    {
        _mqPublishService = mqPublishService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/test/mq/request-response");
        Summary(s =>
        {
            s.Summary = "RabbitMQ 요청-응답 패턴 테스트";
            s.Description = "RabbitMQ를 통해 메시지를 보내고 응답을 받는 요청-응답 패턴을 테스트합니다.";
            s.Responses[200] = "요청-응답 테스트 성공";
            s.Responses[408] = "응답 타임아웃";
            s.Responses[500] = "서버 오류";
        });
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var message = $"안녕하세요! 요청 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var target = "test.reply.queue"; // 응답을 보낼 대상 큐

            _logger.LogInformation("요청-응답 테스트 시작. 대상: {Target}, 메시지: {Message}", target, message);

            // 30초 타임아웃으로 요청-응답 실행
            var response = await _mqPublishService.SendAndWaitForResponseAsync(
                target,
                message,
                TimeSpan.FromSeconds(30),
                ct);

            _logger.LogInformation("응답 수신 완료: {Response}", response);

            Response = new TestMqRequestResponseResponse
            {
                Success = true,
                RequestMessage = message,
                ResponseMessage = response,
                Target = target,
                ResponseTime = DateTime.Now
            };
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "요청-응답 타임아웃 발생");
            HttpContext.Response.StatusCode = 408;
            Response = new TestMqRequestResponseResponse
            {
                Success = false,
                ErrorMessage = "응답 타임아웃이 발생했습니다. 대상 큐가 응답하지 않습니다.",
                RequestMessage = "타임아웃으로 인해 기록되지 않음",
                ResponseTime = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "요청-응답 테스트 중 오류 발생");
            HttpContext.Response.StatusCode = 500;
            Response = new TestMqRequestResponseResponse
            {
                Success = false,
                ErrorMessage = $"오류 발생: {ex.Message}",
                RequestMessage = "오류로 인해 기록되지 않음",
                ResponseTime = DateTime.Now
            };
        }
    }
}

/// <summary>
/// 요청-응답 테스트 응답 DTO
/// </summary>
public class TestMqRequestResponseResponse
{
    /// <summary>
    /// 테스트 성공 여부
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 전송한 요청 메시지
    /// </summary>
    public string RequestMessage { get; set; } = string.Empty;

    /// <summary>
    /// 수신한 응답 메시지
    /// </summary>
    public string? ResponseMessage { get; set; }

    /// <summary>
    /// 대상 큐 이름
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// 응답 수신 시간
    /// </summary>
    public DateTime ResponseTime { get; set; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; set; }
}