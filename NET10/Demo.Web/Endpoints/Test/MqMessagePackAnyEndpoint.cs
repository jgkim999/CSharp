using System.Diagnostics;
using Demo.Application.DTO;
using Demo.Application.Services;
using Demo.Domain;
using FastEndpoints;

namespace Demo.Web.Endpoints.Test;

/// <summary>
/// RabbitMQ Any 방식 메시지 발행 테스트 엔드포인트의 Swagger 문서화를 위한 요약 클래스
/// FastEndpoints의 EndpointSummary를 상속받아 API 문서를 정의합니다
/// </summary>
class MqMessagePackAnyEndpointSummary : EndpointSummary
{
    /// <summary>
    /// MqMessagePackAnyEndpointSummary의 새 인스턴스를 초기화하고 Swagger 문서 정보를 설정합니다
    /// Any 방식은 round-robin으로 하나의 컨슈머만 메시지를 처리합니다
    /// </summary>
    public MqMessagePackAnyEndpointSummary()
    {
        Summary = "MQ Any Message Pack";
        Description = "MQ Publish MessagePack Any message";
        ExampleRequest = new MqPublishRequest()
        {
            Message = "Any message"
        };
        Responses[200] = "success";
        Responses[403] = "forbidden";
    }
}

/// <summary>
/// RabbitMQ Any 방식 메시지 발행을 테스트하는 엔드포인트 클래스
/// FastEndpoints를 사용하여 구현되며, round-robin 방식으로 컨슈머 중 하나만 메시지를 처리합니다
/// OpenTelemetry를 통한 분산 추적을 지원합니다
/// </summary>
public class MqMessagePackAnyEndpoint : Endpoint<MqPublishRequest>
{
    private readonly ILogger<MqMessagePackAnyEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IMqPublishService _mqPublishService;

    /// <summary>
    /// TestMqAnyMessageEndpoint의 새 인스턴스를 초기화합니다
    /// 로거, 텔레메트리 서비스, 메시지 큐 발행 서비스를 주입받습니다
    /// </summary>
    /// <param name="logger">로깅을 위한 ILogger 인스턴스</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService 인스턴스</param>
    /// <param name="mqPublishService">RabbitMQ 메시지 발행을 위한 IMqPublishService 인스턴스</param>
    public MqMessagePackAnyEndpoint(
        ILogger<MqMessagePackAnyEndpoint> logger,
        ITelemetryService telemetryService,
        IMqPublishService mqPublishService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
        _mqPublishService = mqPublishService;
    }

    /// <summary>
    /// 엔드포인트의 라우팅 및 보안 설정을 구성합니다
    /// POST /api/test/mqany 경로로 익명 접근을 허용하며 MqTest 그룹에 속합니다
    /// </summary>
    public override void Configure()
    {
        Post("/api/test/mqany");
        AllowAnonymous();
        //Version(1);
        Group<MqTest>();
        Summary(new MqMessagePackAnyEndpointSummary());
    }

    /// <summary>
    /// RabbitMQ Any 방식 메시지 발행 요청을 비동기적으로 처리합니다
    /// OpenTelemetry Activity를 생성하여 분산 추적을 수행하고, 메시지를 round-robin 방식으로 발행합니다
    /// 처리 결과와 추적 정보를 포함한 응답을 반환합니다
    /// </summary>
    /// <param name="msg">발행할 메시지가 포함된 요청 객체</param>
    /// <param name="ct">작업 취소를 위한 CancellationToken</param>
    /// <returns>메시지 발행 결과와 추적 정보가 포함된 응답</returns>
    public override async Task HandleAsync(MqPublishRequest msg, CancellationToken ct)
    {
        try
        {
            using var activity = _telemetryService.StartActivity(nameof(MqMessagePackAnyEndpoint));
            MqPublishRequest request = new()
            {
                Message = msg.Message
            };
            await _mqPublishService.PublishMessagePackAnyAsync("consumer-any-queue", request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(MqMessagePackAnyEndpoint));
        }
        
        Response = new
        {
            Message = "MQ 테스트가 완료되었습니다. 콘솔 로그를 확인해주세요.",
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

