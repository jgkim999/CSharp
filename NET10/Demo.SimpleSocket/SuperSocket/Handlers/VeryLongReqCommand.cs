using System.Diagnostics;
using Bogus;
using Demo.Application.DTO.Socket;
using Demo.Application.Services;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

public record VeryLongReqCommand(VeryLongReq Packet, string SessionId) : ICommand;

public class VeryLongReqCommandHandler : ICommandHandler<VeryLongReqCommand>
{
    private static readonly Faker _faker = new("ko");
    private readonly ILogger<VeryLongReqCommandHandler> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly ITelemetryService _telemetryService;

    public VeryLongReqCommandHandler(
        ILogger<VeryLongReqCommandHandler> logger,
        ISessionManager sessionManager,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _telemetryService = telemetryService;
    }
    
    public async Task HandleAsync(VeryLongReqCommand command, CancellationToken cancellationToken = default)
    {
        using var activity = _telemetryService.StartActivity("VeryLongReqHandler.HandleAsync", ActivityKind.Consumer, new Dictionary<string, object?>
        {
            ["session.id"] = command.SessionId,
            ["request.data_length"] = command.Packet.Data?.Length ?? 0
        });

        try
        {
            var session = _sessionManager.GetSession(command.SessionId);
            if (session == null)
            {
                _logger.LogWarning("Session not found. SessionID: {SessionID}", command.SessionId);
                activity?.SetTag("error", "Session not found");
                return;
            }

            _logger.LogInformation(
                "VeryLongReq 수신. SessionID: {SessionID}, DataLength: {DataLength}",
                command.SessionId, command.Packet.Data?.Length ?? 0);

            // Bogus를 사용하여 매우 긴 응답 데이터 생성 (약 2000~3000자)
            // static 필드 재사용으로 매번 인스턴스 생성 방지
            var longText = string.Join("\n", new[]
            {
                _faker.Lorem.Paragraphs(10),  // 10개 문단
                _faker.Lorem.Paragraphs(10),  // 10개 문단
                _faker.Lorem.Paragraphs(10),  // 10개 문단
            });

            var response = new VeryLongRes
            {
                Data = $"[서버 응답] 수신한 데이터 길이: {command.Packet.Data?.Length ?? 0}자\n\n{longText}"
            };

            activity?.SetTag("response.data_length", response.Data.Length);

            await session.SendMessagePackAsync(SocketMessageType.VeryLongRes, response);

            _logger.LogInformation(
                "VeryLongRes 전송 완료. SessionID: {SessionID}, ResponseLength: {ResponseLength}",
                session.SessionID, response.Data.Length);

            // VeryLongReq 처리 메트릭 (압축 테스트용 메시지)
            _telemetryService.RecordBusinessMetric(
                "socket.handler.very_long_request",
                1,
                new Dictionary<string, object?>
                {
                    ["request.data_length"] = command.Packet.Data?.Length ?? 0,
                    ["response.data_length"] = response.Data.Length
                });

            _telemetryService.SetActivitySuccess(activity, "VeryLongReq processed successfully");
        }
        catch (ObjectDisposedException ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            _logger.LogError(ex, "{Name} {SessionId}", nameof(VeryLongReqCommandHandler), command.SessionId);
        }
        catch (Exception ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            _logger.LogError(ex, "{Name}", nameof(VeryLongReqCommandHandler));
        }
    }
}
