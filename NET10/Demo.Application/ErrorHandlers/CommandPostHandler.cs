using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class CommandPostHandler : ICommandPostHandler
{
    private readonly ILogger<CommandPostHandler> _logger;

    public CommandPostHandler(ILogger<CommandPostHandler> logger)
    {
        _logger = logger;
    }

    public async Task PostHandleAsync(ICommand command, object? result, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Command 처리 완료: {CommandType}", command.GetType().Name);
        await Task.CompletedTask;
    }
}