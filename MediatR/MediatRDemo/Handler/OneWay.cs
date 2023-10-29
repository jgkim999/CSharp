using MediatR;

namespace MediatrDemo.Handler;

public class OneWay : IRequest
{
}

public class OneWayHandler : IRequestHandler<OneWay>
{
    public Task Handle(OneWay request, CancellationToken cancellationToken)
    {
        // do work
        return Task.CompletedTask;
    }
}