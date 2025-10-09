using Demo.Application.DTO.Socket;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

public record PongCommand(MsgPackPing Packet, string SessionId) : ICommand;

public class PongCommandHandler : ICommandHandler<PongCommand>
{
    private readonly ILogger<PongCommandHandler> _logger;
    private readonly ISessionManager _sessionManager;

    public PongCommandHandler(ILogger<PongCommandHandler> logger, ISessionManager sessionManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
    }

    public async Task HandleAsync(PongCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            DemoSession? session = _sessionManager.GetSession(command.SessionId);
            if (session is null)
            {
                _logger.LogWarning("Session이 없습니다. {SessionId}", command.SessionId);
                return;
            }
            
            var utcNow = DateTime.UtcNow;
            var rtt = (utcNow - command.Packet.ServerDt).TotalMilliseconds;
            
            session.SetLastPong(utcNow, rtt);
            
            _logger.LogInformation("Pong 수신. {MilliSeconds}ms", rtt);
            await Task.CompletedTask;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "PongCommand 처리 중 오류 발생. ObjectDisposedException");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PongCommand 처리 중 오류 발생");
        }
    }
}
