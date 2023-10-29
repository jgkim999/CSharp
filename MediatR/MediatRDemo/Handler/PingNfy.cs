using System.Diagnostics;
using MediatR;

namespace MediatrDemo.Handler;

public class PingNfy : INotification
{
}

public class Pong1 : INotificationHandler<PingNfy>
{
    public Task Handle(PingNfy notification, CancellationToken cancellationToken)
    {
        Debug.WriteLine("Pong 1");
        return Task.CompletedTask;
    }
}

public class Pong2 : INotificationHandler<PingNfy>
{
    public Task Handle(PingNfy notification, CancellationToken cancellationToken)
    {
        Debug.WriteLine("Pong 2");
        return Task.CompletedTask;
    }
}