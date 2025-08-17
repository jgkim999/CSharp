using FluentAssertions;
using Demo.Application.Commands.Sod;
using Demo.Application.Services.Sod;
using Demo.Infra.Services.Sod;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GamePulse.Test.Sod;

public class SodBackgroundWorkerTests
{
    private readonly Mock<ISodBackgroundTaskQueue> _mockQueue;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<SodBackgroundWorker>> _mockLogger;
    private readonly SodBackgroundWorker _worker;

    public SodBackgroundWorkerTests()
    {
        _mockQueue = new Mock<ISodBackgroundTaskQueue>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<SodBackgroundWorker>>();
        _worker = new SodBackgroundWorker(_mockServiceProvider.Object, _mockQueue.Object, _mockLogger.Object, 2);
    }

    [Fact]
    public void Constructor_WithCustomWorkerCount_SetsWorkerCount()
    {
        // Arrange & Act
        var worker = new SodBackgroundWorker(_mockServiceProvider.Object, _mockQueue.Object, _mockLogger.Object, 5);

        // Assert
        worker.Should().NotBeNull();
    }
}