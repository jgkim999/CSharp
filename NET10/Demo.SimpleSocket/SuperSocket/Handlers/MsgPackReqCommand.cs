using System.Diagnostics;

using Demo.Application.Services;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using Demo.SimpleSocketShare;
using Demo.SimpleSocketShare.Messages;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

public record MsgPackReqCommand(MsgPackReq Packet, string SessionId) : ICommand;

public class MsgPackReqCommandHandler : ICommandHandler<MsgPackReqCommand>
{
    private readonly ILogger<MsgPackReqCommandHandler> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly ITelemetryService _telemetryService;

    public MsgPackReqCommandHandler(
        ILogger<MsgPackReqCommandHandler> logger,
        ISessionManager sessionManager,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _telemetryService = telemetryService;
    }
    
    public async Task HandleAsync(MsgPackReqCommand msg, CancellationToken cancellationToken)
    {
        using var activity = _telemetryService.StartActivity("MsgPackReqHandler.HandleAsync", ActivityKind.Consumer, new Dictionary<string, object?>
        {
            ["session.id"] = msg.SessionId,
            ["message.name"] = msg.Packet.Name,
            ["message.length"] = msg.Packet.Message?.Length ?? 0
        });

        try
        {
            var session = _sessionManager.GetSession(msg.SessionId);
            if (session == null)
            {
                _logger.LogError("세션 정보가 없습니다. SessionId: {SessionId}", msg.SessionId);
                activity?.SetTag("error", "Session not found");
                return;
            }

            // MsgPackRes 생성
            var response = new MsgPackRes
            {
                Msg = $"서버에서 받은 메시지: {msg.Packet.Message} (보낸이: {msg.Packet.Name})",
                ProcessDt = DateTime.Now
            };

            activity?.SetTag("response.length", response.Msg.Length);

            await session.SendMessagePackAsync(SocketMessageType.MsgPackResponse, response);
            _logger.LogInformation("MsgPackRes 전송 완료. Msg: {Msg}", response.Msg);

            // MsgPackRequest 처리 완료 메트릭
            _telemetryService.RecordBusinessMetric(
                "socket.handler.msgpack_request",
                1,
                new Dictionary<string, object?>
                {
                    ["request.name_length"] = msg.Packet.Name?.Length ?? 0,
                    ["request.message_length"] = msg.Packet.Message?.Length ?? 0,
                    ["response.message_length"] = response.Msg.Length
                });

            _telemetryService.SetActivitySuccess(activity, "MsgPack request processed successfully");
        }
        catch (ObjectDisposedException ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            _logger.LogError(ex, "세션이 이미 종료되었습니다. SessionId: {SessionId}", msg.SessionId);
        }
        catch (Exception ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            _logger.LogError(ex, "MsgPackReqCommandHandler 에러 발생. SessionId: {SessionId}", msg.SessionId);
        }
    }
}
