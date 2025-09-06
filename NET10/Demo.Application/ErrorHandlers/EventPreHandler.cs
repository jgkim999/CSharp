using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class EventPreHandler : IEventPreHandler
{
    private readonly ILogger<EventPreHandler> _logger;

    public EventPreHandler(ILogger<EventPreHandler> logger)
    {
        _logger = logger;
    }

    public async Task PreHandleAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Event 처리 시작: {EventType}", @event.GetType().Name);
        await Task.CompletedTask;
    }
}