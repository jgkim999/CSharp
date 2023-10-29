using MediatR;

namespace MediatrDemo.Handler;

public class Ping : IRequest<DateTime>
{
}

public class PingHandler : IRequestHandler<Ping, DateTime>
{
    public Task<DateTime> Handle(Ping request, CancellationToken cancellationToken)
    {
        return Task.FromResult(DateTime.Now);
    }
}