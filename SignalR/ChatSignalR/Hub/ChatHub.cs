namespace ChatSignalR.Hub;
using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(false);
            _logger.LogInformation("ChatHub Dispose");
        }
    }

    public override async Task OnConnectedAsync()
    {
        // Handle new connection
        _logger.LogInformation("ChatHub OnConnectAsync");
        await Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Handle disconnection
        _logger.LogInformation("ChatHub OnDisonnectedAsync");
        await Task.CompletedTask;
    }

    public async Task SendMessage(string user, string message)
    {
        _logger.LogInformation("ChatHub SendMessage {0} {1}", user, message);
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinGroup(string groupName)
    {
        _logger.LogInformation("ChatHub JoinGroup {0} {1}", Context.ConnectionId, groupName);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendMessageToGroup(string groupName, string user, string message)
    {
        _logger.LogInformation("ChatHub SendMessageToGroup {0} {1} {2}", groupName, user, message);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message);
    }
}
