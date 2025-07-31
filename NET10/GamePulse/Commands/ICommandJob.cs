namespace GamePulse.Commands;

/// <summary>
/// 
/// </summary>
public interface ICommandJob
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="ct"></param>
    /// <summary>
/// Executes the command asynchronously using the provided service provider and supports cancellation.
/// </summary>
    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}
