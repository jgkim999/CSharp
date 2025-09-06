using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class CommandPreHandler : ICommandPreHandler
{
    private readonly ILogger<CommandPreHandler> _logger;

    public CommandPreHandler(ILogger<CommandPreHandler> logger)
    {
        _logger = logger;
    }

    public async Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Command 처리 시작: {CommandType}", command.GetType().Name);
        await Task.CompletedTask;
    }
}