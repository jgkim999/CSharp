using Demo.Application.DTO;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Domain.Enums;
using Demo.Infra.Configs;
using Demo.Infra.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Concurrent;
using System.Text;
using Testcontainers.RabbitMq;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Services;


/// <summary>
/// RabbitMQ 전송/수신 통합 테스트
/// Testcontainers를 사용하여 실제 RabbitMQ 인스턴스와 테스트
/// </summary>
public class RabbitMqPublishServiceTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IMqMessageHandler> _mockMessageHandler;
    private readonly ConcurrentQueue<string> _receivedMessages;
    private readonly ConcurrentQueue<(object messageObject, Type messageType)> _receivedMessagePackObjects;

    public RabbitMqPublishServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _receivedMessages = new ConcurrentQueue<string>();
        _receivedMessagePackObjects = new ConcurrentQueue<(object, Type)>();

        // RabbitMQ 테스트 컨테이너 설정
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .Build();

        // Mock 서비스들 설정
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), It.IsAny<System.Diagnostics.ActivityKind>(), It.IsAny<System.Diagnostics.ActivityContext?>(), It.IsAny<Dictionary<string, object?>?>()))
            .Returns((System.Diagnostics.Activity?)null);

        _mockMessageHandler = new Mock<IMqMessageHandler>();
        _mockMessageHandler
            .Setup(x => x.HandleAsync(It.IsAny<MqSenderType>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((MqSenderType senderType, string? sender, string? correlationId, string? messageId, string message, CancellationToken ct) =>
                {
                    _receivedMessages.Enqueue(message);
                    _output.WriteLine($"Received message: {message} from {senderType}");
                })
            .Returns(ValueTask.FromResult<string?>(null));

        _mockMessageHandler
            .Setup(x => x.HandleMessagePackAsync(It.IsAny<MqSenderType>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
            .Callback((MqSenderType senderType, string? sender, string? correlationId, string? messageId, object messageObject, Type messageType, CancellationToken ct) =>
                {
                    _receivedMessagePackObjects.Enqueue((messageObject, messageType));
                    _output.WriteLine($"Received MessagePack object: {messageType.Name} from {senderType}");
                })
            .Returns(ValueTask.FromResult<object?>(null));

        // DI 컨테이너 설정
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(_mockTelemetryService.Object);
        services.AddSingleton(_mockMessageHandler.Object);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();
        _output.WriteLine($"RabbitMQ Container Started: {_rabbitMqContainer.GetConnectionString()}");
    }

    public async Task DisposeAsync()
    {
        await _rabbitMqContainer.StopAsync();
        await _serviceProvider.DisposeAsync();
    }

    private RabbitMqConfig CreateRabbitMqConfig()
    {
        return new RabbitMqConfig
        {
            HostName = _rabbitMqContainer.Hostname,
            Port = _rabbitMqContainer.GetMappedPublicPort(5672),
            MultiQueue = "test-queue",
            MultiExchange = "test-exchange",
            UserName = "rabbitmq", // Testcontainers 기본 사용자
            Password = "rabbitmq"  // Testcontainers 기본 비밀번호
        };
    }

    private (RabbitMqConnection, RabbitMqPublishService, RabbitMqConsumerService) CreateServices()
    {
        var config = CreateRabbitMqConfig();
        var logger = _serviceProvider.GetRequiredService<ILogger<RabbitMqConsumerService>>();
        var telemetryService = _serviceProvider.GetRequiredService<ITelemetryService>();
        var messageHandler = _serviceProvider.GetRequiredService<IMqMessageHandler>();

        var connection = new RabbitMqConnection(Options.Create(config), logger);
        var rabbitMqHandler = new RabbitMqHandler(connection, messageHandler,
            _serviceProvider.GetRequiredService<ILogger<RabbitMqHandler>>(), telemetryService);

        var publishService = new RabbitMqPublishService(Options.Create(config), connection,
            rabbitMqHandler, telemetryService, _serviceProvider.GetRequiredService<ILogger<RabbitMqPublishService>>());

        var consumerService = new RabbitMqConsumerService(Options.Create(config), connection,
            rabbitMqHandler, _serviceProvider.GetRequiredService<ILogger<RabbitMqConsumerService>>(), telemetryService);

        return (connection, publishService, consumerService);
    }

    [Fact]
    public async Task PublishMultiAsync_StringMessage_ShouldSendAndReceiveMessage()
    {
        // Arrange
        var (connection, publishService, consumerService) = CreateServices();
        var testMessage = "Hello RabbitMQ Multi!";
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간
            
            // Act
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishMultiAsync(
                config.Value.MultiExchange, testMessage, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessages.Should().NotBeEmpty();
            _receivedMessages.Should().Contain(testMessage);

            _mockMessageHandler.Verify(x => x.HandleAsync(
                MqSenderType.Multi,
                It.IsAny<string?>(),
                correlationId,
                It.IsAny<string?>(),
                testMessage,
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishMultiAsync_MessagePackObject_ShouldSendAndReceiveTypedMessage()
    {
        // Arrange
        var (connection, publishService, consumerService) = CreateServices();
        var testMessagePack = new MqPublishRequest { Message = "Hello MessagePack!" };
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간
            
            // Act
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishMultiAsync(
                config.Value.MultiExchange, testMessagePack, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<MqPublishRequest>();
            messageType.Should().Be<MqPublishRequest>();

            var receivedMessage = (MqPublishRequest)messageObject;
            receivedMessage.Message.Should().Be(testMessagePack.Message);

            _mockMessageHandler.Verify(x => x.HandleMessagePackAsync(
                MqSenderType.Multi,
                It.IsAny<string?>(),
                correlationId,
                It.IsAny<string?>(),
                It.IsAny<MqPublishRequest>(),
                typeof(MqPublishRequest),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishAnyAsync_MessagePackObject_ShouldSendAndReceiveMessage()
    {
        // Arrange
        var (connection, publishService, consumerService) = CreateServices();
        var testMessagePack = new MqPublishRequest2 { CreatedAt = DateTime.UtcNow };
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간

            // Act
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishAnyAsync(
                config.Value.AnyQueue, testMessagePack, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<MqPublishRequest2>();
            messageType.Should().Be<MqPublishRequest2>();

            var receivedMessage = (MqPublishRequest2)messageObject;
            receivedMessage.CreatedAt.Should().BeCloseTo(testMessagePack.CreatedAt, TimeSpan.FromSeconds(1));

            _mockMessageHandler.Verify(x => x.HandleMessagePackAsync(
                MqSenderType.Any,
                It.IsAny<string?>(),
                correlationId,
                It.IsAny<string?>(),
                It.IsAny<MqPublishRequest2>(),
                typeof(MqPublishRequest2),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishUniqueAsync_StringMessage_ShouldSendMessage()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var targetQueue = "unique-target-queue";
        var testMessage = "Hello Unique Queue!";
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Act & Assert - 예외가 발생하지 않으면 성공
            await publishService.PublishUniqueAsync(targetQueue, testMessage, CancellationToken.None, correlationId);

            // Unique 메시지는 특정 대상으로 전송되므로 Consumer 없이도 전송은 성공해야 함
            Assert.True(true); // 예외 없이 실행 완료되면 성공
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishUniqueAsync_MessagePackObject_ShouldSerializeAndSend()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var targetQueue = "unique-messagepack-queue";
        var testMessagePack = new MqPublishRequest { Message = "Hello Unique MessagePack!" };
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Act & Assert
            await publishService.PublishUniqueAsync(targetQueue, testMessagePack, CancellationToken.None, correlationId);

            Assert.True(true); // 예외 없이 실행 완료되면 성공
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Dispose_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testMessage = "Test message";

        // Act
        publishService.Dispose();

        // Assert
        var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => publishService.PublishMultiAsync(config.Value.MultiExchange, testMessage).AsTask());

        await connection.DisposeAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Valid message")]
    [InlineData("한글 메시지 테스트")]
    [InlineData("Special chars: !@#$%^&*()")]
    public async Task PublishMultiAsync_VariousStringInputs_ShouldHandleCorrectly(string message)
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();

        try
        {
            // Act & Assert
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishMultiAsync(config.Value.MultiExchange, message);

            Assert.True(true); // 예외 없이 실행 완료되면 성공
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }
}