using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebDemo.Application.NotifyService;

public class NotifyBackgroundService : BackgroundService
{
    private readonly ILogger<NotifyBackgroundService> _logger;
    private readonly IHubContext<NotifyHub> _hubContext;

    public NotifyBackgroundService(
        ILogger<NotifyBackgroundService> logger,
        IHubContext<NotifyHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                await _hubContext.Clients.All.SendAsync(
                    "NotifyMessage",
                    $"Server time: {DateTime.Now}",
                    cancellationToken: stoppingToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"{nameof(NotifyBackgroundService)} exception");
        }
    }
}
