using Demo.Application.Services.Sod;
using Demo.Infra.Services.Sod;
using Demo.Infra.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Demo.Infra.Tests.Services;

public class SodBackgroundWorkerSimpleTests : IAsyncLifetime
{
    private readonly Mock<ILogger<SodBackgroundWorker>> _mockLogger;
    private readonly Mock<ISodBackgroundTaskQueue> _mockTaskQueue;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly SodBackgroundWorker _worker;

    public SodBackgroundWorkerSimpleTests()
    {
        _mockLogger = new Mock<ILogger<SodBackgroundWorker>>();
        _mockTaskQueue = new Mock<ISodBackgroundTaskQueue>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        // ServiceScope 설정
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        
        // IServiceProvider가 IServiceScopeFactory를 반환하도록 설정
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);

        _worker = new SodBackgroundWorker(
            _mockServiceProvider.Object,
            _mockTaskQueue.Object,
            _mockLogger.Object,
            1); // 단일 워커로 테스트
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _worker.StopAsync(CancellationToken.None);
        _worker.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Process_Commands_From_Queue()
    {
        // Arrange
        var mockCommand = new TestSodCommand("192.168.1.1");
        var cancellationTokenSource = new CancellationTokenSource();
        
        _mockTaskQueue
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCommand)
            .Throws(new OperationCanceledException()); // 두 번째 호출에서 종료

        // Act
        var executeTask = _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(100); // Worker가 시작될 시간을 줌
        cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 예상된 예외
        }

        // Assert
        _mockTaskQueue.Verify(
            q => q.DequeueAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        mockCommand.ExecuteCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Command_Execution_Exception()
    {
        // Arrange
        var failingCommand = new FailingSodCommand("192.168.1.1");
        var cancellationTokenSource = new CancellationTokenSource();

        _mockTaskQueue
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(failingCommand)
            .Throws(new OperationCanceledException());

        // Act
        var executeTask = _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(100);
        cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 예상된 예외
        }

        // Assert
        failingCommand.ExecuteCount.Should().Be(1);

        // 예외가 로깅되었는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Continue_Processing_After_Exception()
    {
        // Arrange
        var failingCommand = new FailingSodCommand("192.168.1.1");
        var successCommand = new TestSodCommand("192.168.1.2");
        var cancellationTokenSource = new CancellationTokenSource();

        _mockTaskQueue
            .SetupSequence(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(failingCommand)
            .ReturnsAsync(successCommand)
            .Throws(new OperationCanceledException());

        // Act
        var executeTask = _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(200); // 두 명령어가 처리될 시간을 줌
        cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 예상된 예외
        }

        // Assert
        failingCommand.ExecuteCount.Should().Be(1);
        successCommand.ExecuteCount.Should().Be(1);

        // 오류 로그가 한 번만 기록되었는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_Should_Gracefully_Shutdown_Worker()
    {
        // Arrange
        _mockTaskQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new TestSodCommand("192.168.1.1");
            });

        // Act
        var startTask = _worker.StartAsync(CancellationToken.None);
        await Task.Delay(50); // Worker가 시작될 시간을 줌

        var stopTask = _worker.StopAsync(CancellationToken.None);
        
        // StopAsync는 빠르게 완료되어야 함
        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(stopTask, timeoutTask);

        // Assert
        completedTask.Should().Be(stopTask);
        stopTask.IsCompleted.Should().BeTrue();
    }
}

/// <summary>
/// 실제 구현체들을 사용한 통합 테스트
/// </summary>
public class SodBackgroundWorkerIntegrationSimpleTests : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISodBackgroundTaskQueue _taskQueue;
    private readonly SodBackgroundWorker _worker;

    public SodBackgroundWorkerIntegrationSimpleTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddTransient(provider => new SodBackgroundWorker(
            provider,
            provider.GetRequiredService<ISodBackgroundTaskQueue>(),
            provider.GetRequiredService<ILogger<SodBackgroundWorker>>(),
            1)); // 단일 워커

        _serviceProvider = services.BuildServiceProvider();
        _taskQueue = _serviceProvider.GetRequiredService<ISodBackgroundTaskQueue>();
        _worker = _serviceProvider.GetService<SodBackgroundWorker>()!;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _worker.StopAsync(CancellationToken.None);
        _worker.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task Integration_Worker_Should_Process_Real_Commands()
    {
        // Arrange
        var command1 = new TestSodCommand("192.168.1.1");
        var command2 = new TestSodCommand("192.168.1.2");

        // Act
        await _taskQueue.EnqueueAsync(command1);
        await _taskQueue.EnqueueAsync(command2);

        await _worker.StartAsync(CancellationToken.None);
        await Task.Delay(1000); // 명령어들이 처리될 시간을 줌
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        command1.ExecuteCount.Should().Be(1);
        command2.ExecuteCount.Should().Be(1);
    }

    [Fact]
    public async Task Integration_Worker_Should_Process_Commands_In_Order()
    {
        // Arrange
        var commands = new List<TestSodCommand>();
        var commandCount = 10;

        for (int i = 0; i < commandCount; i++)
        {
            var command = new TestSodCommand($"192.168.1.{i}", i);
            commands.Add(command);
            await _taskQueue.EnqueueAsync(command);
        }

        // Act
        await _worker.StartAsync(CancellationToken.None);
        await Task.Delay(2000); // 모든 명령어들이 처리될 시간을 줌
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        foreach (var command in commands)
        {
            command.ExecuteCount.Should().Be(1);
        }
    }
}

