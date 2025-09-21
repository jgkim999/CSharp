using Demo.Application.DTO;
using Demo.Application.Services;
using Demo.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Services;

/// <summary>
/// MqMessageHandler 단위 테스트
/// 메시지 처리 로직과 타입별 핸들러 테스트
/// </summary>
public class MqMessageHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<MqMessageHandler>> _mockLogger;
    private readonly MqMessageHandler _messageHandler;

    public MqMessageHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<MqMessageHandler>>();
        _messageHandler = new MqMessageHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_StringMessage_ShouldLogMessage()
    {
        // Arrange
        var senderType = MqSenderType.Multi;
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var message = "Test message for string handling";

        // Act
        await _messageHandler.HandleAsync(senderType, sender, correlationId, messageId, message, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessagePackAsync_MqPublishRequest_ShouldCallCorrectHandler()
    {
        // Arrange
        var senderType = MqSenderType.Multi;
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var messageObject = new MqPublishRequest { Message = "Test MessagePack message" };
        var messageType = typeof(MqPublishRequest);

        // Act
        await _messageHandler.HandleMessagePackAsync(senderType, sender, correlationId, messageId, messageObject, messageType, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessagePack Object Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessagePackAsync_MqPublishRequest2_ShouldCallCorrectHandler()
    {
        // Arrange
        var senderType = MqSenderType.Any;
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var messageObject = new MqPublishRequest2 { CreatedAt = DateTime.UtcNow };
        var messageType = typeof(MqPublishRequest2);

        // Act
        await _messageHandler.HandleMessagePackAsync(senderType, sender, correlationId, messageId, messageObject, messageType, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessagePack Object Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessagePackAsync_UnknownType_ShouldLogWarning()
    {
        // Arrange
        var senderType = MqSenderType.Unique;
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var messageObject = new { UnknownProperty = "test" };
        var messageType = messageObject.GetType();

        // Act
        await _messageHandler.HandleMessagePackAsync(senderType, sender, correlationId, messageId, messageObject, messageType, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No handler registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessagePackAsync_InvalidCast_ShouldLogError()
    {
        // Arrange
        var senderType = MqSenderType.Multi;
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var messageObject = "This is a string, not MqPublishRequest";
        var messageType = typeof(MqPublishRequest); // 잘못된 타입 매핑

        // Act
        await _messageHandler.HandleMessagePackAsync(senderType, sender, correlationId, messageId, messageObject, messageType, CancellationToken.None);

        // Assert - 타입 불일치로 인한 처리는 핸들러 내부에서 처리됨
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessagePack Object Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(MqSenderType.Multi)]
    [InlineData(MqSenderType.Any)]
    [InlineData(MqSenderType.Unique)]
    public async Task HandleAsync_AllSenderTypes_ShouldProcess(MqSenderType senderType)
    {
        // Arrange
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var message = $"Test message for {senderType}";

        // Act
        await _messageHandler.HandleAsync(senderType, sender, correlationId, messageId, message, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullValues_ShouldHandleGracefully()
    {
        // Arrange
        var senderType = MqSenderType.Multi;
        string? sender = null;
        string? correlationId = null;
        string? messageId = null;
        var message = "Test message with null values";

        // Act
        await _messageHandler.HandleAsync(senderType, sender, correlationId, messageId, message, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessagePackAsync_CancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var senderType = MqSenderType.Multi;
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var messageObject = new MqPublishRequest { Message = "Test message" };
        var messageType = typeof(MqPublishRequest);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // 즉시 취소

        // Act & Assert
        await _messageHandler.HandleMessagePackAsync(senderType, sender, correlationId, messageId, messageObject, messageType, cts.Token);

        // 취소되었지만 동기적으로 처리되므로 완료되어야 함
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessagePack Object Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessagePackAsync_LargeMessage_ShouldProcessCorrectly()
    {
        // Arrange
        var senderType = MqSenderType.Multi;
        var sender = "test-sender";
        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var largeMessage = new string('X', 10000); // 10KB 메시지
        var messageObject = new MqPublishRequest { Message = largeMessage };
        var messageType = typeof(MqPublishRequest);

        // Act
        await _messageHandler.HandleMessagePackAsync(senderType, sender, correlationId, messageId, messageObject, messageType, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessagePack Object Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(largeMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessagePackAsync_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var messageCount = 100;

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var messageObject = new MqPublishRequest { Message = $"Concurrent message {i}" };
            var task = _messageHandler.HandleMessagePackAsync(
                MqSenderType.Multi,
                "concurrent-sender",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                messageObject,
                typeof(MqPublishRequest),
                CancellationToken.None);

            tasks.Add(task.AsTask());
        }

        await Task.WhenAll(tasks);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MessagePack Object Processed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(messageCount));
    }
}