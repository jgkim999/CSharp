using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class EventPostHandler : IEventPostHandler
{
    private readonly ILogger<EventPostHandler> _logger;

    public EventPostHandler(ILogger<EventPostHandler> logger)
    {
        _logger = logger;
    }

    public async Task PostHandleAsync(IEvent @event, object? result, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Event 처리 완료: {EventType}", @event.GetType().Name);
        await Task.CompletedTask;
    }
}