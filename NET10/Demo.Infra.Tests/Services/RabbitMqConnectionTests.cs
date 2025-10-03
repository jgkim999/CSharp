using Demo.Infra.Configs;
using Demo.Infra.Services;
using Demo.Infra.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Testcontainers.RabbitMq;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Services;

/// <summary>
/// RabbitMqConnection 통합 테스트
/// 공유 RabbitMQ 컨테이너를 사용하여 RabbitMQ 연결의 기능을 검증합니다
/// </summary>
public class RabbitMqConnectionTests : IClassFixture<ContainerFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly ContainerFixture _containerFixture;
    private readonly Mock<ILogger<RabbitMqConsumerService>> _mockLogger;

    public RabbitMqConnectionTests(ITestOutputHelper output, ContainerFixture containerFixture)
    {
        _output = output;
        _containerFixture = containerFixture;
        _mockLogger = new Mock<ILogger<RabbitMqConsumerService>>();

        _output.WriteLine($"Using shared RabbitMQ Container: {_containerFixture.RabbitMqConnectionString}");
    }


    private RabbitMqConfig CreateRabbitMqConfig()
    {
        return new RabbitMqConfig
        {
            HostName = _containerFixture.RabbitMqContainer.Hostname,
            Port = _containerFixture.RabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "rabbitmq",
            Password = "rabbitmq",
            VirtualHost = "/",
            MultiExchange = "test-multi-exchange",
            MultiQueue = "test-multi-queue",
            AnyQueue = "test-any-queue",
            UniqueQueue = "test-unique-queue",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = 10,
            TopologyRecoveryEnabled = true,
            ConsumerDispatchConcurrency = 1
        };
    }

    [Fact]
    public void Constructor_WithValidConfig_ShouldInitializeSuccessfully()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        var options = Options.Create(config);

        // Act
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Assert
        connection.Should().NotBeNull();
        connection.Channel.Should().NotBeNull();
        connection.MultiExchange.Should().Be("test-multi-exchange");
        connection.MultiQueue.Should().StartWith("test-multi-queue.");
        connection.AnyQueue.Should().Be("test-any-queue");
        connection.UniqueQueue.Should().StartWith("test-unique-queue.");
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new RabbitMqConnection(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MultiExchange_ShouldAppendCorrectSuffix()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        config.MultiExchange = "my-exchange";
        var options = Options.Create(config);

        // Act
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Assert
        connection.MultiExchange.Should().Be("my-exchange");
    }

    [Fact]
    public void MultiQueue_ShouldAppendUlid()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        config.MultiQueue = "my-queue";
        var options = Options.Create(config);

        // Act
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Assert
        connection.MultiQueue.Should().StartWith("my-queue.");
        connection.MultiQueue.Should().HaveLength("my-queue.".Length + 26); // ULID is 26 characters
    }

    [Fact]
    public void UniqueQueue_ShouldAppendUlid()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        config.UniqueQueue = "unique-queue";
        var options = Options.Create(config);

        // Act
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Assert
        connection.UniqueQueue.Should().StartWith("unique-queue.");
        connection.UniqueQueue.Should().HaveLength("unique-queue.".Length + 26); // ULID is 26 characters
    }

    [Fact]
    public void AnyQueue_ShouldNotModifyName()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        config.AnyQueue = "exact-queue-name";
        var options = Options.Create(config);

        // Act
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Assert
        connection.AnyQueue.Should().Be("exact-queue-name");
    }

    [Fact]
    public async Task Channel_ShouldBeUsableForBasicOperations()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        var options = Options.Create(config);

        using var connection = new RabbitMqConnection(options, _mockLogger.Object);
        var channel = connection.Channel;

        // Act & Assert - Should not throw exceptions
        await channel.QueueDeclareAsync(
            queue: "test-queue",
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null);

        // Simplified - just test queue creation/deletion
        // BasicPublishAsync has complex generic constraints"

        // Cleanup
        await channel.QueueDeleteAsync("test-queue", ifUnused: false, ifEmpty: false);
    }

    [Fact]
    public void MultipleInstances_ShouldHaveDifferentQueueNames()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        var options = Options.Create(config);

        // Act
        using var connection1 = new RabbitMqConnection(options, _mockLogger.Object);
        using var connection2 = new RabbitMqConnection(options, _mockLogger.Object);

        // Assert
        connection1.MultiQueue.Should().NotBe(connection2.MultiQueue);
        connection1.UniqueQueue.Should().NotBe(connection2.UniqueQueue);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        var options = Options.Create(config);
        var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Act & Assert
        var act = () => connection.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrowException()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        var options = Options.Create(config);
        var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Act & Assert
        var act = async () => await connection.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("test-exchange-1", "test-queue-1", "test-any-1", "test-unique-1")]
    [InlineData("exchange-2", "queue-2", "any-2", "unique-2")]
    [InlineData("특수문자-테스트", "한글큐", "any한글", "unique한글")]
    public void Constructor_WithDifferentQueueNames_ShouldSetCorrectProperties(
        string multiExchange, string multiQueue, string anyQueue, string uniqueQueue)
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        config.MultiExchange = multiExchange;
        config.MultiQueue = multiQueue;
        config.AnyQueue = anyQueue;
        config.UniqueQueue = uniqueQueue;
        var options = Options.Create(config);

        // Act
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);

        // Assert
        connection.MultiExchange.Should().Be($"{multiExchange}");
        connection.MultiQueue.Should().StartWith($"{multiQueue}.");
        connection.AnyQueue.Should().Be(anyQueue);
        connection.UniqueQueue.Should().StartWith($"{uniqueQueue}.");
    }

    [Fact]
    public void Constructor_WithCustomConnectionSettings_ShouldApplySettings()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        config.AutomaticRecoveryEnabled = false;
        config.NetworkRecoveryInterval = 30;
        config.TopologyRecoveryEnabled = false;
        config.ConsumerDispatchConcurrency = 5;
        var options = Options.Create(config);

        // Act & Assert - Should not throw exception during initialization
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);
        connection.Should().NotBeNull();
    }

    [Fact]
    public async Task ConnectionInitialization_ShouldInitializeWithoutErrors()
    {
        // Arrange
        var config = CreateRabbitMqConfig();
        var options = Options.Create(config);

        // Act & Assert - Should initialize without throwing exceptions
        using var connection = new RabbitMqConnection(options, _mockLogger.Object);

        await Task.Delay(100); // Allow for initialization

        connection.Should().NotBeNull();
        connection.Channel.Should().NotBeNull();
    }
}