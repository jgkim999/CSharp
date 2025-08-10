using System.Diagnostics;
using Demo.Application.Commands;

namespace GamePulse.Sod.Commands;

public abstract class SodCommand : ICommandJob
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SodCommand"/> class with the specified client IP address and optional parent activity.
    /// </summary>
    /// <param name="clientIp">The IP address of the client associated with the command.</param>
    /// <param name="parentActivity">An optional parent <see cref="Activity"/> for tracing or diagnostics.</param>
    protected SodCommand(string clientIp, Activity? parentActivity)
    {
        ClientIp = clientIp;
        ParentActivity = parentActivity;
    }

    public string ClientIp { get; set; }

    public Activity? ParentActivity { get; set; }

    /// <summary>
    /// Executes the command asynchronously using the provided service provider and supports cancellation.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies required by the command.</param>
    /// <param name="ct">A cancellation token to observe while executing the command.</param>
    /// <returns>A task representing the asynchronous execution of the command.</returns>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}
