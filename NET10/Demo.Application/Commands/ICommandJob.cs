namespace Demo.Application.Commands;

public interface ICommandJob
{
    /// <summary>
    /// Executes the command asynchronously using the provided service provider and supports cancellation.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="ct"></param>
    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}
