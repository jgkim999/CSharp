using System.Diagnostics;
using Demo.Application.DTO;
using Demo.Application.Services;
using Demo.Domain;
using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// RabbitMQ MessagePack 방식 메시지 발행 테스트 엔드포인트의 Swagger 문서화를 위한 요약 클래스
/// FastEndpoints의 EndpointSummary를 상속받아 API 문서를 정의합니다
/// </summary>
class TestMqMessagePackSummary : EndpointSummary
{
    /// <summary>
    /// TestMqMessagePackSummary의 새 인스턴스를 초기화하고 Swagger 문서 정보를 설정합니다
    /// MessagePack 직렬화를 사용하여 타입 정보와 함께 메시지를 전송합니다
    /// </summary>
    public TestMqMessagePackSummary()
    {
        Summary = "MQ MessagePack Message Test";
        Description = "MQ Publish MessagePack message with type information";
        ExampleRequest = new MqPublishRequest()
        {
            Message = "MessagePack test message"
        };
        Responses[200] = "success";
        Responses[403] = "forbidden";
    }
}

/// <summary>
/// RabbitMQ MessagePack 방식 메시지 발행을 테스트하는 엔드포인트 클래스
/// FastEndpoints를 사용하여 구현되며, MessagePack 직렬화와 타입 정보를 헤더에 포함하여 전송합니다
/// OpenTelemetry를 통한 분산 추적을 지원합니다
/// </summary>
public class TestMqMessagePackEndpoint : Endpoint<MqPublishRequest>
{
    private readonly ILogger<TestMqMessagePackEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IMqPublishService _mqPublishService;

    /// <summary>
    /// TestMqMessagePackEndpoint의 새 인스턴스를 초기화합니다
    /// 로거, 텔레메트리 서비스, 메시지 큐 발행 서비스를 주입받습니다
    /// </summary>
    /// <param name="logger">로깅을 위한 ILogger 인스턴스</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService 인스턴스</param>
    /// <param name="mqPublishService">RabbitMQ 메시지 발행을 위한 IMqPublishService 인스턴스</param>
    public TestMqMessagePackEndpoint(
        ILogger<TestMqMessagePackEndpoint> logger,
        ITelemetryService telemetryService,
        IMqPublishService mqPublishService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _mqPublishService = mqPublishService;
    }

    /// <summary>
    /// 엔드포인트의 라우팅 및 보안 설정을 구성합니다
    /// POST /api/test/mqmessagepack 경로로 익명 접근을 허용하며 MqTest 그룹에 속합니다
    /// </summary>
    public override void Configure()
    {
        Post("/api/test/mqmessagepack");
        AllowAnonymous();
        Group<MqTest>();
        Summary(new TestMqMessagePackSummary());
    }

    /// <summary>
    /// RabbitMQ MessagePack 방식 메시지 발행 요청을 비동기적으로 처리합니다
    /// OpenTelemetry Activity를 생성하여 분산 추적을 수행하고, MessagePack으로 직렬화하여 타입 정보와 함께 발행합니다
    /// 처리 결과와 추적 정보를 포함한 응답을 반환합니다
    /// </summary>
    /// <param name="msg">발행할 메시지가 포함된 요청 객체</param>
    /// <param name="ct">작업 취소를 위한 CancellationToken</param>
    /// <returns>메시지 발행 결과와 추적 정보가 포함된 응답</returns>
    public override async Task HandleAsync(MqPublishRequest msg, CancellationToken ct)
    {
        try
        {
            using var activity = _telemetryService.StartActivity(nameof(TestMqMessagePackEndpoint));

            // MessagePack으로 메시지 발행 (타입 정보 포함)
            await _mqPublishService.PublishMultiAsync("consumer-multi-exchange", msg, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(TestMqMessagePackEndpoint));
        }

        Response = new
        {
            Message = "MessagePack MQ 테스트가 완료되었습니다. 콘솔 로그를 확인해주세요.",
            MessageType = typeof(MqPublishRequest).FullName,
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
