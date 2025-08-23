using Demo.Domain.Commands;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Domain.Tests.Commands;

public class ICommandJobTests
{
    private readonly Mock<ICommandJob> _mockCommand;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ICommandJobTests()
    {
        _mockCommand = new Mock<ICommandJob>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Complete_Successfully()
    {
        // Arrange
        var cancellationToken = _cancellationTokenSource.Token;

        _mockCommand
            .Setup(c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _mockCommand.Object.ExecuteAsync(_mockServiceProvider.Object, cancellationToken);

        // Assert
        _mockCommand.Verify(
            c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        _cancellationTokenSource.Cancel();
        var cancellationToken = _cancellationTokenSource.Token;

        _mockCommand
            .Setup(c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _mockCommand.Object.ExecuteAsync(_mockServiceProvider.Object, cancellationToken));

        _mockCommand.Verify(
            c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Service_Provider_For_Dependencies()
    {
        // Arrange
        var mockService = new Mock<ITestService>();
        var cancellationToken = _cancellationTokenSource.Token;

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ITestService)))
            .Returns(mockService.Object);

        _mockCommand
            .Setup(c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _mockCommand.Object.ExecuteAsync(_mockServiceProvider.Object, cancellationToken);

        // Assert
        _mockCommand.Verify(
            c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Exception_During_Execution()
    {
        // Arrange
        var cancellationToken = _cancellationTokenSource.Token;
        var expectedMessage = "Command execution failed";

        _mockCommand
            .Setup(c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mockCommand.Object.ExecuteAsync(_mockServiceProvider.Object, cancellationToken));

        exception.Message.Should().Be(expectedMessage);

        _mockCommand.Verify(
            c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Multiple_Concurrent_Executions()
    {
        // Arrange
        var cancellationToken = _cancellationTokenSource.Token;
        var tasks = new List<Task>();

        _mockCommand
            .Setup(c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_mockCommand.Object.ExecuteAsync(_mockServiceProvider.Object, cancellationToken));
        }

        await Task.WhenAll(tasks);

        // Assert
        _mockCommand.Verify(
            c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken),
            Times.Exactly(5));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Complete_Even_With_Null_Service_Provider()
    {
        // Arrange
        var cancellationToken = _cancellationTokenSource.Token;

        _mockCommand
            .Setup(c => c.ExecuteAsync(null!, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _mockCommand.Object.ExecuteAsync(null!, cancellationToken);

        // Assert
        _mockCommand.Verify(
            c => c.ExecuteAsync(null!, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Respect_Cancellation_Token_Timeout()
    {
        // Arrange
        _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));
        var cancellationToken = _cancellationTokenSource.Token;

        _mockCommand
            .Setup(c => c.ExecuteAsync(_mockServiceProvider.Object, cancellationToken))
            .Returns(async () =>
            {
                await Task.Delay(200, cancellationToken); // This should be cancelled
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _mockCommand.Object.ExecuteAsync(_mockServiceProvider.Object, cancellationToken));
    }

    // Test interface for dependency injection testing
    public interface ITestService
    {
        Task DoSomethingAsync();
    }
}

// Concrete implementation for testing command job behavior
public class TestCommandJob : ICommandJob
{
    private readonly List<string> _executionLog;

    public TestCommandJob()
    {
        _executionLog = new List<string>();
    }

    public IReadOnlyList<string> ExecutionLog => _executionLog.AsReadOnly();

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        _executionLog.Add($"Started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        
        // Simulate some work
        await Task.Delay(10, ct);
        
        _executionLog.Add($"Completed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
    }
}

public class ConcreteCommandJobTests
{
    [Fact]
    public async Task TestCommandJob_Should_Log_Execution_Timeline()
    {
        // Arrange
        var command = new TestCommandJob();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var cancellationToken = CancellationToken.None;

        // Act
        await command.ExecuteAsync(serviceProvider, cancellationToken);

        // Assert
        command.ExecutionLog.Should().HaveCount(2);
        command.ExecutionLog[0].Should().StartWith("Started at");
        command.ExecutionLog[1].Should().StartWith("Completed at");
    }

    [Fact]
    public async Task TestCommandJob_Should_Respect_Cancellation()
    {
        // Arrange
        var command = new TestCommandJob();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Cancel immediately
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => command.ExecuteAsync(serviceProvider, cancellationTokenSource.Token));
    }
}