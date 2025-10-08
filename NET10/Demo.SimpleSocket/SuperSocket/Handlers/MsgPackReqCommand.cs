using Demo.Application.DTO.Socket;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

public record MsgPackReqCommand(MsgPackReq Packet, DemoSession Session) : ICommand;

public class MsgPackReqCommandHandler : ICommandHandler<MsgPackReqCommand>
{
    private readonly ILogger<MsgPackReqCommandHandler> _logger;

    public MsgPackReqCommandHandler(ILogger<MsgPackReqCommandHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task HandleAsync(MsgPackReqCommand msg, CancellationToken cancellationToken)
    {
        // MsgPackRes 생성
        var response = new MsgPackRes
        {
            Msg = $"서버에서 받은 메시지: {msg.Packet.Message} (보낸이: {msg.Packet.Name})",
            ProcessDt = DateTime.Now
        };

        await msg.Session.SendMessagePackAsync(SocketMessageType.MsgPackResponse, response);
        _logger.LogInformation("MsgPackRes 전송 완료. Msg: {Msg}", response.Msg);
    }
}
