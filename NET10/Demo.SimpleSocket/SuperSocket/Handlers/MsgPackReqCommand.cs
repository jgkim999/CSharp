using Demo.Application.DTO.Socket;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

public record MsgPackReqCommand(MsgPackReq Packet, string SessionId) : ICommand;

public class MsgPackReqCommandHandler : ICommandHandler<MsgPackReqCommand>
{
    private readonly ILogger<MsgPackReqCommandHandler> _logger;
    private readonly ISessionManager _sessionManager;

    public MsgPackReqCommandHandler(ILogger<MsgPackReqCommandHandler> logger, ISessionManager sessionManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
    }
    
    public async Task HandleAsync(MsgPackReqCommand msg, CancellationToken cancellationToken)
    {
        try
        {
            var session = _sessionManager.GetSession(msg.SessionId);
            if (session == null)
            {
                _logger.LogError("세션 정보가 없습니다. SessionId: {SessionId}", msg.SessionId);
                return;
            }

            // MsgPackRes 생성
            var response = new MsgPackRes
            {
                Msg = $"서버에서 받은 메시지: {msg.Packet.Message} (보낸이: {msg.Packet.Name})",
                ProcessDt = DateTime.Now
            };

            await session.SendMessagePackAsync(SocketMessageType.MsgPackResponse, response);
            _logger.LogInformation("MsgPackRes 전송 완료. Msg: {Msg}", response.Msg);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "세션이 이미 종료되었습니다. SessionId: {SessionId}", msg.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MsgPackReqCommandHandler 에러 발생. SessionId: {SessionId}", msg.SessionId);
        }
    }
}
