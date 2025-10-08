using Demo.Application.Handlers.Commands.Sod;

namespace Demo.Infra.Tests.TestHelpers;

public class TestSodCommand : SodCommand
{
    public int Id { get; }
    public int ExecuteCount { get; private set; }

    public TestSodCommand(string clientIp, int id = 0) : base(clientIp, null)
    {
        Id = id;
    }

    public override async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        ExecuteCount++;
    }
}

public class FailingSodCommand : SodCommand
{
    public int ExecuteCount { get; private set; }

    public FailingSodCommand(string clientIp) : base(clientIp, null)
    {
    }

    public override async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        ExecuteCount++;
        throw new InvalidOperationException("Test exception");
    }
}