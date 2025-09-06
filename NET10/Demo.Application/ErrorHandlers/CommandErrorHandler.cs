using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class CommandErrorHandler : ICommandErrorHandler
{
    private readonly ILogger<CommandErrorHandler> _logger;

    public CommandErrorHandler(ILogger<CommandErrorHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleErrorAsync(ICommand command, object? result, Exception exception, CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception, "Command 처리 중 오류 발생: {CommandType}", command.GetType().Name);
        await Task.CompletedTask;
    }
}