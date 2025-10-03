using Demo.Application.DTO;
using Demo.Application.Models;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Domain.Enums;
using Demo.Infra.Configs;
using Demo.Infra.Services;
using Demo.Infra.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Concurrent;
using System.Text;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Services;


/// <summary>
/// RabbitMQ 전송/수신 통합 테스트
/// Collection을 사용하여 모든 테스트가 하나의 컨테이너를 공유
/// </summary>
[Collection("Container Collection")]
public class RabbitMqPublishServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly ContainerFixture _containerFixture;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IMqMessageHandler> _mockMessageHandler;
    private readonly ConcurrentQueue<string> _receivedMessages;
    private readonly ConcurrentQueue<(object messageObject, Type messageType)> _receivedMessagePackObjects;
    private readonly RabbitMqConnection _sharedConnection;

    public RabbitMqPublishServiceTests(ITestOutputHelper output, ContainerFixture containerFixture)
    {
        _output = output;
        _containerFixture = containerFixture;
        _receivedMessages = new ConcurrentQueue<string>();
        _receivedMessagePackObjects = new ConcurrentQueue<(object, Type)>();

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
            .Setup(x => x.HandleBinaryMessageAsync(It.IsAny<MqSenderType>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
            .Callback((MqSenderType senderType, string? sender, string? correlationId, string? messageId, object messageObject, Type messageType, CancellationToken ct) =>
                {
                    _receivedMessagePackObjects.Enqueue((messageObject, messageType));
                    _output.WriteLine($"Received MessagePack object: {messageType.Name} from {senderType}");
                })
            .Returns(ValueTask.FromResult<object?>(null));

        // 공유 connection 생성
        var config = CreateRabbitMqConfig();
        var tempLogger = new Mock<ILogger<RabbitMqConsumerService>>().Object;
        _sharedConnection = new RabbitMqConnection(Options.Create(config), tempLogger);

        // DI 컨테이너 설정
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(_mockTelemetryService.Object);
        services.AddSingleton(_mockMessageHandler.Object);
        services.AddSingleton(_sharedConnection);

        // RabbitMqHandler를 DI 컨테이너에 등록 (RabbitMqConsumerService가 scope에서 요청함)
        services.AddScoped<RabbitMqHandler>(provider =>
            new RabbitMqHandler(
                provider.GetRequiredService<RabbitMqConnection>(),
                provider.GetRequiredService<IMqMessageHandler>(),
                provider.GetRequiredService<ILogger<RabbitMqHandler>>(),
                provider.GetRequiredService<ITelemetryService>()));

        _serviceProvider = services.BuildServiceProvider();

        _output.WriteLine($"Using shared RabbitMQ Container: {_containerFixture.RabbitMqConnectionString}");
    }

    private RabbitMqConfig CreateRabbitMqConfig()
    {
        return new RabbitMqConfig
        {
            HostName = _containerFixture.RabbitMqContainer.Hostname,
            Port = _containerFixture.RabbitMqContainer.GetMappedPublicPort(5672),
            MultiQueue = "test-queue",
            MultiExchange = "test-exchange",
            AnyQueue = "test-any-queue",
            UserName = "rabbitmq", // Testcontainers 기본 사용자
            Password = "rabbitmq"  // Testcontainers 기본 비밀번호
        };
    }

    private (RabbitMqConnection, RabbitMqPublishService, RabbitMqConsumerService) CreateServices()
    {
        var config = CreateRabbitMqConfig();
        var logger = _serviceProvider.GetRequiredService<ILogger<RabbitMqConsumerService>>();
        var telemetryService = _serviceProvider.GetRequiredService<ITelemetryService>();

        // 공유 connection 사용
        var connection = _sharedConnection;

        // PublishService용 RabbitMqHandler 생성 (기존 방식 유지)
        var messageHandler = _serviceProvider.GetRequiredService<IMqMessageHandler>();
        var rabbitMqHandler = new RabbitMqHandler(connection, messageHandler,
            _serviceProvider.GetRequiredService<ILogger<RabbitMqHandler>>(), telemetryService);

        var publishService = new RabbitMqPublishService(Options.Create(config), connection,
            rabbitMqHandler, telemetryService, _serviceProvider.GetRequiredService<ILogger<RabbitMqPublishService>>());

        var consumerService = new RabbitMqConsumerService(Options.Create(config), connection,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(), _serviceProvider.GetRequiredService<ILogger<RabbitMqConsumerService>>());

        return (connection, publishService, consumerService);
    }

    [Fact]
    public async Task PublishMultiAsync_StringMessage_ShouldSendAndReceiveMessage()
    {
        // Arrange - 이전 테스트의 메시지 초기화
        while (_receivedMessagePackObjects.TryDequeue(out _)) { }

        var (connection, publishService, consumerService) = CreateServices();
        var testMessage = "Hello RabbitMQ Multi!";
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간

            // Act
            await publishService.PublishMessagePackMultiAsync(
                connection.MultiExchange, testMessage, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<string>();
            messageType.Should().Be<string>();

            var receivedMessage = (string)messageObject;
            receivedMessage.Should().Be(testMessage);

            _mockMessageHandler.Verify(x => x.HandleBinaryMessageAsync(
                MqSenderType.Multi,
                It.IsAny<string?>(),
                correlationId,
                It.IsAny<string?>(),
                It.IsAny<string>(),
                typeof(string),
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
        // Arrange - 이전 테스트의 메시지 초기화
        while (_receivedMessagePackObjects.TryDequeue(out _)) { }

        var (connection, publishService, consumerService) = CreateServices();
        var testMessagePack = new MqPublishRequest { Message = "Hello MessagePack!" };
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간

            // Act
            await publishService.PublishMessagePackMultiAsync(
                connection.MultiExchange, testMessagePack, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<MqPublishRequest>();
            messageType.Should().Be<MqPublishRequest>();

            var receivedMessage = (MqPublishRequest)messageObject;
            receivedMessage.Message.Should().Be(testMessagePack.Message);

            _mockMessageHandler.Verify(x => x.HandleBinaryMessageAsync(
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
        // Arrange - 이전 테스트의 메시지 초기화
        while (_receivedMessagePackObjects.TryDequeue(out _)) { }

        var (connection, publishService, consumerService) = CreateServices();
        var testMessagePack = new MqPublishRequest2 { CreatedAt = DateTime.UtcNow };
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간

            // Act
            await publishService.PublishMessagePackAnyAsync(
                connection.AnyQueue, testMessagePack, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<MqPublishRequest2>();
            messageType.Should().Be<MqPublishRequest2>();

            var receivedMessage = (MqPublishRequest2)messageObject;
            receivedMessage.CreatedAt.Should().BeCloseTo(testMessagePack.CreatedAt, TimeSpan.FromSeconds(1));

            _mockMessageHandler.Verify(x => x.HandleBinaryMessageAsync(
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
            await publishService.PublishMessagePackUniqueAsync(targetQueue, testMessage, CancellationToken.None, correlationId);

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
            await publishService.PublishMessagePackUniqueAsync(targetQueue, testMessagePack, CancellationToken.None, correlationId);

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
            () => publishService.PublishMessagePackMultiAsync(config.Value.MultiExchange, testMessage).AsTask());

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
            await publishService.PublishMessagePackMultiAsync(config.Value.MultiExchange, message);

            Assert.True(true); // 예외 없이 실행 완료되면 성공
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    #region ProtoBuf Tests

    [Fact]
    public async Task PublishProtoBufMultiAsync_StringMessage_ShouldSendAndReceiveMessage()
    {
        // Arrange - 이전 테스트의 메시지 초기화
        while (_receivedMessagePackObjects.TryDequeue(out _)) { }

        var (connection, publishService, consumerService) = CreateServices();
        var testMessage = "Hello RabbitMQ ProtoBuf!";
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간

            // Act
            await publishService.PublishProtoBufMultiAsync(
                connection.MultiExchange, testMessage, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<string>();
            messageType.Should().Be<string>();

            var receivedMessage = (string)messageObject;
            receivedMessage.Should().Be(testMessage);

            _mockMessageHandler.Verify(x => x.HandleBinaryMessageAsync(
                MqSenderType.Multi,
                It.IsAny<string?>(),
                correlationId,
                It.IsAny<string?>(),
                It.IsAny<string>(),
                typeof(string),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishProtoBufMultiAsync_ProtoBufObject_ShouldSendAndReceiveTypedMessage()
    {
        // Arrange - 이전 테스트의 메시지 초기화
        while (_receivedMessagePackObjects.TryDequeue(out _)) { }

        var (connection, publishService, consumerService) = CreateServices();
        var testProtoBuf = new MqPublishProtoBufRequest
        {
            Message = "Hello ProtoBuf!",
            Id = 12345,
            CreatedAt = DateTime.UtcNow
        };
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간

            // Act
            await publishService.PublishProtoBufMultiAsync(
                connection.MultiExchange, testProtoBuf, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<MqPublishProtoBufRequest>();
            messageType.Should().Be<MqPublishProtoBufRequest>();

            var receivedMessage = (MqPublishProtoBufRequest)messageObject;
            receivedMessage.Message.Should().Be(testProtoBuf.Message);
            receivedMessage.Id.Should().Be(testProtoBuf.Id);
            receivedMessage.CreatedAt.Should().BeCloseTo(testProtoBuf.CreatedAt, TimeSpan.FromSeconds(1));

            _mockMessageHandler.Verify(x => x.HandleBinaryMessageAsync(
                MqSenderType.Multi,
                It.IsAny<string?>(),
                correlationId,
                It.IsAny<string?>(),
                It.IsAny<MqPublishProtoBufRequest>(),
                typeof(MqPublishProtoBufRequest),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishProtoBufMultiAsync_ComplexProtoBufObject_ShouldSendAndReceiveWithAllFields()
    {
        // Arrange - 이전 테스트의 메시지 초기화
        while (_receivedMessagePackObjects.TryDequeue(out _)) { }

        var (connection, publishService, consumerService) = CreateServices();
        var testProtoBuf = new MqPublishProtoBufRequest2
        {
            Name = "홍길동",
            Email = "hong@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Consumer 시작
            _ = consumerService.StartAsync(CancellationToken.None);
            await Task.Delay(1000); // Consumer 준비 시간

            // Act
            await publishService.PublishProtoBufMultiAsync(
                connection.MultiExchange, testProtoBuf, CancellationToken.None, correlationId);
            await Task.Delay(2000); // 메시지 처리 시간

            // Assert
            _receivedMessagePackObjects.Should().NotBeEmpty();
            var (messageObject, messageType) = _receivedMessagePackObjects.First();

            messageObject.Should().BeOfType<MqPublishProtoBufRequest2>();
            messageType.Should().Be<MqPublishProtoBufRequest2>();

            var receivedMessage = (MqPublishProtoBufRequest2)messageObject;
            receivedMessage.Name.Should().Be(testProtoBuf.Name);
            receivedMessage.Email.Should().Be(testProtoBuf.Email);
            receivedMessage.IsActive.Should().Be(testProtoBuf.IsActive);
            receivedMessage.CreatedAt.Should().BeCloseTo(testProtoBuf.CreatedAt, TimeSpan.FromSeconds(1));

            _mockMessageHandler.Verify(x => x.HandleBinaryMessageAsync(
                MqSenderType.Multi,
                It.IsAny<string?>(),
                correlationId,
                It.IsAny<string?>(),
                It.IsAny<MqPublishProtoBufRequest2>(),
                typeof(MqPublishProtoBufRequest2),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Simple ProtoBuf message")]
    [InlineData("한글 ProtoBuf 메시지")]
    [InlineData("ProtoBuf Special chars: !@#$%^&*()")]
    [InlineData("")]
    public async Task PublishProtoBufMultiAsync_VariousStringInputs_ShouldHandleCorrectly(string message)
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testProtoBuf = new MqPublishProtoBufRequest { Message = message, Id = 999 };

        try
        {
            // Act & Assert
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishProtoBufMultiAsync(config.Value.MultiExchange, testProtoBuf);

            Assert.True(true); // 예외 없이 실행 완료되면 성공
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishProtoBufMultiAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testProtoBuf = new MqPublishProtoBufRequest { Message = "Test ProtoBuf" };

        // Act
        publishService.Dispose();

        // Assert
        var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => publishService.PublishProtoBufMultiAsync(config.Value.MultiExchange, testProtoBuf).AsTask());

        await connection.DisposeAsync();
    }

    #endregion

    #region MemoryPack Tests

    [Fact]
    public async Task PublishMemoryPackMultiAsync_StringMessage_ShouldSendWithoutError()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testMemoryPack = new MqPublishMemoryPackRequest { Message = "Test MemoryPack Message", Id = 123 };
        var correlationId = Ulid.NewUlid().ToString();

        try
        {
            // Act & Assert - MemoryPack 직렬화가 성공적으로 실행되면 예외 없이 완료
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishMemoryPackMultiAsync(config.Value.MultiExchange, testMemoryPack, correlationId: correlationId);

            // 예외 없이 실행 완료되면 성공
            Assert.True(true);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishMemoryPackMultiAsync_MemoryPackObject_ShouldSendWithoutError()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testMemoryPack = new MqPublishMemoryPackRequest
        {
            Message = "MemoryPack Complex Object Test",
            Id = 456,
            CreatedAt = DateTime.UtcNow
        };
        var correlationId = Ulid.NewUlid().ToString();

        try
        {
            // Act & Assert - MemoryPack 직렬화가 성공적으로 실행되면 예외 없이 완료
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishMemoryPackMultiAsync(config.Value.MultiExchange, testMemoryPack, correlationId: correlationId);

            // 예외 없이 실행 완료되면 성공
            Assert.True(true);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishMemoryPackMultiAsync_ComplexMemoryPackObject_ShouldSendWithoutError()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testMemoryPack = new MqPublishMemoryPackRequest2
        {
            Name = "John Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var correlationId = Ulid.NewUlid().ToString();

        try
        {
            // Act & Assert - MemoryPack 직렬화가 성공적으로 실행되면 예외 없이 완료
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishMemoryPackMultiAsync(config.Value.MultiExchange, testMemoryPack, correlationId: correlationId);

            // 예외 없이 실행 완료되면 성공
            Assert.True(true);
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Simple MemoryPack message")]
    [InlineData("한글 MemoryPack 메시지")]
    [InlineData("MemoryPack Special chars: !@#$%^&*()")]
    [InlineData("")]
    public async Task PublishMemoryPackMultiAsync_VariousStringInputs_ShouldHandleCorrectly(string message)
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testMemoryPack = new MqPublishMemoryPackRequest { Message = message, Id = 999 };

        try
        {
            // Act & Assert
            var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
            await publishService.PublishMemoryPackMultiAsync(config.Value.MultiExchange, testMemoryPack);

            Assert.True(true); // 예외 없이 실행 완료되면 성공
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishMemoryPackMultiAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var testMemoryPack = new MqPublishMemoryPackRequest { Message = "Test MemoryPack" };

        // Act
        publishService.Dispose();

        // Assert
        var config = _serviceProvider.GetRequiredService<IOptions<RabbitMqConfig>>();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => publishService.PublishMemoryPackMultiAsync(config.Value.MultiExchange, testMemoryPack).AsTask());

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task PublishMemoryPackAndWaitForResponseAsync_ValidRequestResponse_ShouldSerializeSuccessfully()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var request = new MemoryPackRequest
        {
            Id = Ulid.NewUlid().ToString(),
            Message = "Test Request",
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, string> { { "key1", "value1" } }
        };

        try
        {
            // Act & Assert - Request-Response는 Consumer가 없으면 타임아웃이 발생하는 것이 정상 동작
            // 여기서는 직렬화가 성공적으로 이루어지는지만 검증
            var serializeTask = Task.Run(async () =>
            {
                try
                {
                    await publishService.PublishMemoryPackAndWaitForResponseAsync<MemoryPackRequest, MemoryPackResponse>(
                        "test-memorypack-rpc-queue",
                        request,
                        TimeSpan.FromMilliseconds(100)); // 짧은 타임아웃
                }
                catch (TimeoutException)
                {
                    // 타임아웃은 예상된 동작 (응답할 Consumer가 없음)
                    return true;
                }
                catch (Exception ex)
                {
                    // 타임아웃 외의 예외는 직렬화 문제를 의미
                    _output.WriteLine($"Unexpected exception: {ex.Message}");
                    return false;
                }
                return true;
            });

            var result = await serializeTask;
            Assert.True(result, "MemoryPack serialization should succeed even if no consumer responds");
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task PublishMemoryPackAndWaitForResponseAsync_WithTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        var (connection, publishService, _) = CreateServices();
        var request = new MemoryPackRequest
        {
            Id = Ulid.NewUlid().ToString(),
            Message = "Timeout Test",
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Act & Assert - 존재하지 않는 큐에 메시지를 보내면 타임아웃 발생
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await publishService.PublishMemoryPackAndWaitForResponseAsync<MemoryPackRequest, MemoryPackResponse>(
                    "non-existent-queue",
                    request,
                    TimeSpan.FromSeconds(2));
            });
        }
        finally
        {
            publishService.Dispose();
            await connection.DisposeAsync();
        }
    }

    #endregion
}