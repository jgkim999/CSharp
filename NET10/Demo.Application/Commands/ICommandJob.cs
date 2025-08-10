namespace Demo.Application.Commands;

public interface ICommandJob
{
    /// <summary>
    /// Executes the command asynchronously using the provided service provider and supports cancellation.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <summary>
/// Executes the command asynchronously using the provided service provider and supports cancellation.
/// </summary>
/// <param name="ct">A token to monitor for cancellation requests.</param>
/// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}
