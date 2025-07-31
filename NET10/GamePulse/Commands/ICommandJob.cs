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
    /// <returns></returns>
    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}
