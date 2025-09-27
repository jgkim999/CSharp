using Demo.Application.DTO;
using Demo.Domain.Enums;
using Demo.Infra.Configs;
using FluentAssertions;
using MessagePack;
using RabbitMQ.Client;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Services;

/// <summary>
/// RabbitMQ 퍼블리시 서비스 단위 테스트 (간소화된 Mock 기반)
/// </summary>
public class RabbitMqPublishServiceUnitTests
{
    private readonly ITestOutputHelper _output;

    public RabbitMqPublishServiceUnitTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RabbitMqConfig_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new RabbitMqConfig();

        // Assert
        config.Port.Should().Be(5672);
        config.UserName.Should().Be("guest");
        config.Password.Should().Be("guest");
        config.HostName.Should().Be(string.Empty);
        config.MultiExchange.Should().Be(string.Empty);
        config.MultiQueue.Should().Be(string.Empty);
    }

    [Fact]
    public void RabbitMqConfig_ShouldAllowCustomValues()
    {
        // Arrange & Act
        var config = new RabbitMqConfig
        {
            HostName = "custom-host",
            Port = 15672,
            UserName = "admin",
            Password = "secret",
            MultiExchange = "custom-exchange",
            MultiQueue = "custom-queue"
        };

        // Assert
        config.HostName.Should().Be("custom-host");
        config.Port.Should().Be(15672);
        config.UserName.Should().Be("admin");
        config.Password.Should().Be("secret");
        config.MultiExchange.Should().Be("custom-exchange");
        config.MultiQueue.Should().Be("custom-queue");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Normal message")]
    [InlineData("한글 메시지")]
    [InlineData("Special chars: !@#$%^&*()")]
    [InlineData("Very long message: " + "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.")]
    public void MessageValidation_ShouldHandleVariousStringInputs(string message)
    {
        // Arrange
        var messageObject = new MqPublishRequest { Message = message };

        // Act & Assert - 이 테스트는 단순히 객체 생성과 속성 설정이 정상적으로 작동하는지 확인
        messageObject.Message.Should().Be(message);
        _output.WriteLine($"Successfully handled message: '{message}' (Length: {message.Length})");
    }

    [Fact]
    public void MqPublishRequest_ShouldSerializeToMessagePack()
    {
        // Arrange
        var message = new MqPublishRequest { Message = "Test MessagePack serialization" };

        // Act
        var serialized = MessagePackSerializer.Serialize(message);
        var deserialized = MessagePackSerializer.Deserialize<MqPublishRequest>(serialized);

        // Assert
        serialized.Should().NotBeEmpty();
        deserialized.Should().NotBeNull();
        deserialized.Message.Should().Be(message.Message);
    }

    [Fact]
    public void MqPublishRequest2_ShouldSerializeToMessagePack()
    {
        // Arrange
        var message = new MqPublishRequest2 { CreatedAt = DateTime.UtcNow };

        // Act
        var serialized = MessagePackSerializer.Serialize(message);
        var deserialized = MessagePackSerializer.Deserialize<MqPublishRequest2>(serialized);

        // Assert
        serialized.Should().NotBeEmpty();
        deserialized.Should().NotBeNull();
        deserialized.CreatedAt.Should().BeCloseTo(message.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void CorrelationId_ShouldBeValidGuid()
    {
        // Arrange & Act
        var correlationId = Guid.NewGuid().ToString();

        // Assert
        Guid.TryParse(correlationId, out _).Should().BeTrue();
        correlationId.Should().NotBeNullOrEmpty();
        correlationId.Length.Should().Be(36); // Standard GUID string length
    }

    [Theory]
    [InlineData(MqSenderType.Multi)]
    [InlineData(MqSenderType.Any)]
    [InlineData(MqSenderType.Unique)]
    public void MqSenderType_ShouldHaveValidValues(MqSenderType senderType)
    {
        // Act & Assert
        senderType.Should().BeDefined();
        senderType.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BasicProperties_Headers_ShouldSupportMessageTypeAndSenderType()
    {
        // Arrange
        var properties = new BasicProperties();
        properties.Headers = new Dictionary<string, object?>();

        // Act
        properties.Headers["MessageType"] = typeof(MqPublishRequest).FullName;
        properties.Headers["SenderType"] = MqSenderType.Multi.ToString();
        properties.CorrelationId = Guid.NewGuid().ToString();

        // Assert
        properties.Headers.Should().ContainKey("MessageType");
        properties.Headers.Should().ContainKey("SenderType");
        properties.Headers["MessageType"].Should().Be(typeof(MqPublishRequest).FullName);
        properties.Headers["SenderType"].Should().Be(MqSenderType.Multi.ToString());
        properties.CorrelationId.Should().NotBeNullOrEmpty();
    }
}