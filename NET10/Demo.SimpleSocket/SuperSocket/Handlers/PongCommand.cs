using System.Diagnostics;
using Demo.Application.DTO.Socket;
using Demo.Application.Services;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

public record PongCommand(MsgPackPing Packet, string SessionId) : ICommand;

public class PongCommandHandler : ICommandHandler<PongCommand>
{
    private readonly ILogger<PongCommandHandler> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly ITelemetryService _telemetryService;

    public PongCommandHandler(
        ILogger<PongCommandHandler> logger,
        ISessionManager sessionManager,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _telemetryService = telemetryService;
    }

    public async Task HandleAsync(PongCommand command, CancellationToken cancellationToken = default)
    {
        using var activity = _telemetryService.StartActivity("PongHandler.HandleAsync", ActivityKind.Consumer, new Dictionary<string, object?>
        {
            ["session.id"] = command.SessionId
        });

        try
        {
            DemoSession? session = _sessionManager.GetSession(command.SessionId);
            if (session is null)
            {
                _logger.LogWarning("Session이 없습니다. {SessionId}", command.SessionId);
                activity?.SetTag("error", "Session not found");
                return;
            }

            var utcNow = DateTime.UtcNow;
            var rtt = (utcNow - command.Packet.ServerDt).TotalMilliseconds;

            session.SetLastPong(utcNow, rtt);

            activity?.SetTag("rtt.milliseconds", rtt);

            // RTT 메트릭 기록
            _telemetryService.RecordBusinessMetric(
                "socket.handler.pong_rtt",
                (long)rtt,
                new Dictionary<string, object?>
                {
                    ["session.id"] = command.SessionId
                });

            _logger.LogInformation("Pong 수신. {MilliSeconds}ms", rtt);
            _telemetryService.SetActivitySuccess(activity, "Pong received successfully");
            await Task.CompletedTask;
        }
        catch (ObjectDisposedException ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            _logger.LogError(ex, "PongCommand 처리 중 오류 발생. ObjectDisposedException");
        }
        catch (Exception ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            _logger.LogError(ex, "PongCommand 처리 중 오류 발생");
        }
    }
}
