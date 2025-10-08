using Demo.Infra.Services.Sod;
using Demo.Infra.Tests.TestHelpers;
using FluentAssertions;
using System.Diagnostics;
using Demo.Application.Handlers.Commands.Sod;

namespace Demo.Infra.Tests.Services;

public class SodBackgroundTaskQueueSimpleTests
{
    private readonly SodBackgroundTaskQueue _queue;

    public SodBackgroundTaskQueueSimpleTests()
    {
        _queue = new SodBackgroundTaskQueue();
    }

    [Fact]
    public async Task EnqueueAsync_Should_Enqueue_Command_Successfully()
    {
        // Arrange
        var command = new TestSodCommand("192.168.1.1");

        // Act
        await _queue.EnqueueAsync(command);

        // Assert
        // 큐에 항목이 추가되었는지 확인하기 위해 DequeueAsync 호출
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var dequeuedCommand = await _queue.DequeueAsync(cancellationTokenSource.Token);
        
        dequeuedCommand.Should().NotBeNull();
        dequeuedCommand.Should().Be(command);
    }

    [Fact]
    public async Task DequeueAsync_Should_Wait_When_Queue_Is_Empty()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.DequeueAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task DequeueAsync_Should_Return_Commands_In_FIFO_Order()
    {
        // Arrange
        var command1 = new TestSodCommand("192.168.1.1");
        var command2 = new TestSodCommand("192.168.1.2");
        var command3 = new TestSodCommand("192.168.1.3");

        // Act
        await _queue.EnqueueAsync(command1);
        await _queue.EnqueueAsync(command2);
        await _queue.EnqueueAsync(command3);

        var cancellationToken = CancellationToken.None;
        var dequeued1 = await _queue.DequeueAsync(cancellationToken);
        var dequeued2 = await _queue.DequeueAsync(cancellationToken);
        var dequeued3 = await _queue.DequeueAsync(cancellationToken);

        // Assert
        dequeued1.Should().Be(command1);
        dequeued2.Should().Be(command2);
        dequeued3.Should().Be(command3);
    }

    [Fact]
    public async Task TryEnqueue_Should_Return_True_For_Successful_Enqueue()
    {
        // Arrange
        var command = new TestSodCommand("192.168.1.1");

        // Act
        var result = _queue.TryEnqueue(command);

        // Assert
        result.Should().BeTrue();

        // 큐에서 명령어가 검색되는지 확인
        var dequeued = await _queue.DequeueAsync(CancellationToken.None);
        dequeued.Should().Be(command);
    }

    [Fact]
    public void TryDequeue_Should_Return_False_When_Queue_Is_Empty()
    {
        // Act
        var result = _queue.TryDequeue(out var command);

        // Assert
        result.Should().BeFalse();
        command.Should().BeNull();
    }

    [Fact]
    public async Task TryDequeue_Should_Return_True_When_Command_Available()
    {
        // Arrange
        var command = new TestSodCommand("192.168.1.1");
        await _queue.EnqueueAsync(command);

        // Act
        var result = _queue.TryDequeue(out var dequeuedCommand);

        // Assert
        result.Should().BeTrue();
        dequeuedCommand.Should().NotBeNull();
        dequeuedCommand.Should().Be(command);
    }

    [Fact]
    public async Task Queue_Should_Handle_Multiple_Concurrent_Enqueue_Operations()
    {
        // Arrange
        var commandCount = 100;
        var commands = Enumerable.Range(0, commandCount)
            .Select(i => new TestSodCommand($"192.168.1.{i}"))
            .ToList();

        // Act
        var enqueueTasks = commands.Select(cmd => _queue.EnqueueAsync(cmd));
        await Task.WhenAll(enqueueTasks);

        // Assert
        var dequeuedCommands = new List<SodCommand>();
        var cancellationToken = CancellationToken.None;

        for (int i = 0; i < commandCount; i++)
        {
            var command = await _queue.DequeueAsync(cancellationToken);
            dequeuedCommands.Add(command);
        }

        dequeuedCommands.Should().HaveCount(commandCount);
        dequeuedCommands.Should().BeEquivalentTo(commands);
    }

    [Fact]
    public async Task Queue_Should_Handle_High_Throughput()
    {
        // Arrange
        var commandCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act - 대량의 명령어를 빠르게 enqueue
        var enqueueTasks = Enumerable.Range(0, commandCount)
            .Select(i => _queue.EnqueueAsync(new TestSodCommand($"192.168.1.{i % 255}")));

        await Task.WhenAll(enqueueTasks);

        // 모든 명령어를 dequeue
        var dequeueTasks = Enumerable.Range(0, commandCount)
            .Select(_ => _queue.DequeueAsync(CancellationToken.None));

        await Task.WhenAll(dequeueTasks);

        stopwatch.Stop();

        // Assert
        // 1,000개 명령어 처리가 2초 이내에 완료되어야 함
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task Queue_Should_Maintain_Order_Under_Load()
    {
        // Arrange
        var commandCount = 100;
        var orderedCommands = new List<TestSodCommand>();

        for (int i = 0; i < commandCount; i++)
        {
            orderedCommands.Add(new TestSodCommand($"192.168.1.{i}", i));
        }

        // Act
        // 순서대로 enqueue
        foreach (var command in orderedCommands)
        {
            await _queue.EnqueueAsync(command);
        }

        // 순서대로 dequeue
        var dequeuedCommands = new List<TestSodCommand>();
        for (int i = 0; i < commandCount; i++)
        {
            var command = await _queue.DequeueAsync(CancellationToken.None);
            dequeuedCommands.Add((TestSodCommand)command);
        }

        // Assert
        for (int i = 0; i < commandCount; i++)
        {
            dequeuedCommands[i].Id.Should().Be(i);
        }
    }
}

