using OpenTelemetry;

using System.Diagnostics;
using System.Threading.Channels;

namespace JsonToLog.Services;

public class LogSendProcessor : BackgroundService
{
    private readonly Channel<LogSendTask> _logChannel = Channel.CreateUnbounded<LogSendTask>();
    private readonly ILogger<LogSendProcessor> _logger;
    
    public LogSendProcessor(ILogger<LogSendProcessor> logger)
    {
        _logger = logger;
    }
    
    public async Task SendLogAsync(LogSendTask task)
    {
        await _logChannel.Writer.WriteAsync(task);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var item in _logChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        Baggage.Current = item.PropagationContext.Baggage;
                        using var activity = ActivityService.StartActivity("LogSendProcessor.ExecuteAsync", ActivityKind.Consumer, item.PropagationContext.ActivityContext);
                        // Process each log item
                        _logger.LogInformation("Processing log data: {@LogData}", item);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "An error occurred while processing log data: {@LogData}", item.LogData);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
                _logger.LogInformation("Log processing has been cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing log data.");
            }
        }
    }
}