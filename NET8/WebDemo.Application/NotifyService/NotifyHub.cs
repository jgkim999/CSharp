using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace WebDemo.Application.NotifyService;

public class NotifyHub : Hub
{
    private readonly ILogger<NotifyHub> _logger;

    public NotifyHub(ILogger<NotifyHub> logger)
    {
        _logger = logger;
    }

    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("NotifyMessage", message);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected.{connectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation("Client Disconnected.{connectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
