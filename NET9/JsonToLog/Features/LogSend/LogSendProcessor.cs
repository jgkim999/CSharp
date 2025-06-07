using JsonToLog.Services;

using OpenTelemetry;

using System.Diagnostics;
using System.Threading.Channels;

namespace JsonToLog.Features.LogSend;

public class LogSendProcessor : BackgroundService
{
    private readonly Channel<LogSendTask> _logChannel = Channel.CreateUnbounded<LogSendTask>();
    private readonly ILogger<LogSendProcessor> _logger;
    private readonly LogSendMetrics _logSendMetrics;
    private readonly ILogRepository _logRepository;
    
    public LogSendProcessor(
        ILogger<LogSendProcessor> logger,
        LogSendMetrics logSendMetrics,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logSendMetrics = logSendMetrics;
        _logRepository = logRepository;
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
                        long startTime = Stopwatch.GetTimestamp();
                        Baggage.Current = item.PropagationContext.Baggage;
                        using var activity = ActivityService.StartActivity("LogSendProcessor.ExecuteAsync", ActivityKind.Consumer, item.PropagationContext.ActivityContext);
                        // Process each log item
                        await _logRepository.SendLogAsync(item);
                        _logSendMetrics.RecordLogSendDuration(Stopwatch.GetElapsedTime(startTime));
                        _logger.LogInformation("Processing log data: {@LogData} {ElapsedTime}", item.LogData, Stopwatch.GetElapsedTime(startTime).TotalMilliseconds);
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