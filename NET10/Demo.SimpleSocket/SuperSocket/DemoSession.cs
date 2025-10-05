using SuperSocket.Connection;
using SuperSocket.Server;

namespace Demo.SimpleSocket.SuperSocket;

public class DemoSession : AppSession
{
    private readonly ILogger<DemoSession> _logger;

    public DemoSession(ILogger<DemoSession> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when the session is connected.
    /// Session Call Sequence #1
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    protected override async ValueTask OnSessionConnectedAsync()
    {
        _logger.LogInformation("#1 DemoSession connected. {SessionID}", SessionID);
        await ValueTask.CompletedTask;
    }

    /// <summary>
    /// Called when the session is closed.
    /// Session Call Sequence #3
    /// </summary>
    /// <param name="e">The close event arguments containing the reason for closing.</param>
    /// <returns>
    /// A task representing the async operation.
    /// </returns>
    protected override async ValueTask OnSessionClosedAsync(CloseEventArgs e)
    {
        _logger.LogInformation("#3 DemoSession closed. {SessionID} {Reason}", SessionID, e.Reason.ToString());
        await ValueTask.CompletedTask;
    }

    /// <summary>Sends binary data to the client asynchronously.</summary>
    /// <param name="data">The binary data to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async send operation.</returns>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await Connection.SendAsync(data, cancellationToken);
    }

    /// <summary>
    /// Called when the session is reset. Derived classes can override this method to perform additional cleanup.
    /// </summary>
    protected override void Reset()
    {
        _logger.LogInformation("DemoSession Reset. {SessionID}", SessionID);
    }

    /// <summary>Closes the session asynchronously.</summary>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    public override async ValueTask CloseAsync()
    {
        _logger.LogInformation("DemoSession. {SessionID}", SessionID);
        await base.CloseAsync();
    }
}
