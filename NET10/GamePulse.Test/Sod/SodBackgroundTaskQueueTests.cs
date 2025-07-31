using FluentAssertions;
using GamePulse.Sod.Commands;
using GamePulse.Sod.Services;

namespace GamePulse.Test.Sod;

public class TestSodCommand : SodCommand
{
    public TestSodCommand() : base("127.0.0.1", null) { }

    public override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

public class SodBackgroundTaskQueueTests
{
    private readonly SodBackgroundTaskQueue _queue;

    public SodBackgroundTaskQueueTests()
    {
        _queue = new SodBackgroundTaskQueue();
    }

    [Fact]
    public async Task EnqueueAsync_DequeueAsync_ShouldWorkCorrectly()
    {
        // Arrange
        var command = new TestSodCommand();

        // Act
        await _queue.EnqueueAsync(command);
        var result = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        result.Should().Be(command);
    }

    [Fact]
    public async Task DequeueAsync_MultipleItems_ShouldReturnInOrder()
    {
        // Arrange
        var command1 = new TestSodCommand();
        var command2 = new TestSodCommand();

        // Act
        await _queue.EnqueueAsync(command1);
        await _queue.EnqueueAsync(command2);

        var result1 = await _queue.DequeueAsync(CancellationToken.None);
        var result2 = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        result1.Should().Be(command1);
        result2.Should().Be(command2);
    }
}