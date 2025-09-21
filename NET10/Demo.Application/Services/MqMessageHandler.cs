using Demo.Domain;
using Demo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

public class MqMessageHandler : IMqMessageHandler
{
    private readonly ILogger<MqMessageHandler> _logger;
     
    public MqMessageHandler(ILogger<MqMessageHandler> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask HandleAsync(
        MqSenderType senderType, 
        string? sender,
        string? correlationId,
        string? messageId,
        string message,
        CancellationToken ct)
    {
        _logger.LogInformation("Message Processed. {Message}", message);
        await Task.CompletedTask;
    }
}