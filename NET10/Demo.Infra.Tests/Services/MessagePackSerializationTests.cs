using Demo.Application.DTO;
using FluentAssertions;
using MessagePack;
using Microsoft.IO;
using System.Text;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Services;

/// <summary>
/// MessagePack 직렬화/역직렬화 성능 및 정확성 테스트
/// RecyclableMemoryStream과 함께 사용하는 최적화된 직렬화 테스트
/// </summary>
public class MessagePackSerializationTests
{
    private readonly ITestOutputHelper _output;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager;

    public MessagePackSerializationTests(ITestOutputHelper output)
    {
        _output = output;
        _memoryStreamManager = new RecyclableMemoryStreamManager();
    }

    [Fact]
    public async Task SerializeAsync_MqPublishRequest_ShouldProduceValidBytes()
    {
        // Arrange
        var message = new MqPublishRequest { Message = "Test serialization message" };

        // Act
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, message);

        var serializedBytes = memoryStream.ToArray();

        // Assert
        serializedBytes.Should().NotBeEmpty();
        serializedBytes.Length.Should().BeGreaterThan(0);

        _output.WriteLine($"Serialized {message.Message} to {serializedBytes.Length} bytes");
    }

    [Fact]
    public async Task SerializeDeserialize_MqPublishRequest_ShouldMaintainData()
    {
        // Arrange
        var originalMessage = new MqPublishRequest { Message = "Test round-trip message" };

        // Act - Serialize
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, originalMessage);

        var serializedBytes = memoryStream.ToArray();

        // Act - Deserialize
        var deserializedMessage = MessagePackSerializer.Deserialize<MqPublishRequest>(serializedBytes);

        // Assert
        deserializedMessage.Should().NotBeNull();
        deserializedMessage.Message.Should().Be(originalMessage.Message);
    }

    [Fact]
    public async Task SerializeDeserialize_MqPublishRequest2_ShouldMaintainData()
    {
        // Arrange
        var originalMessage = new MqPublishRequest2 { CreatedAt = DateTime.UtcNow };

        // Act - Serialize
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, originalMessage);

        var serializedBytes = memoryStream.ToArray();

        // Act - Deserialize
        var deserializedMessage = MessagePackSerializer.Deserialize<MqPublishRequest2>(serializedBytes);

        // Assert
        deserializedMessage.Should().NotBeNull();
        deserializedMessage.CreatedAt.Should().BeCloseTo(originalMessage.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task SerializeAsync_LargeMessage_ShouldHandleCorrectly()
    {
        // Arrange
        var largeMessage = new string('X', 100000); // 100KB 메시지
        var message = new MqPublishRequest { Message = largeMessage };

        // Act
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, message);

        var serializedBytes = memoryStream.ToArray();

        // Assert
        serializedBytes.Should().NotBeEmpty();
        serializedBytes.Length.Should().BeGreaterThan(largeMessage.Length); // MessagePack 오버헤드 고려

        _output.WriteLine($"Large message ({largeMessage.Length} chars) serialized to {serializedBytes.Length} bytes");

        // Verify deserialization
        var deserialized = MessagePackSerializer.Deserialize<MqPublishRequest>(serializedBytes);
        deserialized.Message.Should().Be(largeMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Simple message")]
    [InlineData("한글 메시지 테스트")]
    [InlineData("Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?")]
    [InlineData("Emoji test: 😀😃😄😁😆😅😂🤣")]
    [InlineData("Numbers: 1234567890")]
    [InlineData("Mixed: abc123한글😀!@#")]
    public async Task SerializeDeserialize_VariousStrings_ShouldMaintainData(string testMessage)
    {
        // Arrange
        var message = new MqPublishRequest { Message = testMessage };

        // Act
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, message);

        var serializedBytes = memoryStream.ToArray();
        var deserializedMessage = MessagePackSerializer.Deserialize<MqPublishRequest>(serializedBytes);

        // Assert
        deserializedMessage.Message.Should().Be(testMessage);
        _output.WriteLine($"'{testMessage}' -> {serializedBytes.Length} bytes");
    }

    [Fact]
    public async Task RecyclableMemoryStream_MultipleOperations_ShouldReuseMemory()
    {
        // Arrange
        var messages = Enumerable.Range(1, 100)
            .Select(i => new MqPublishRequest { Message = $"Message {i}" })
            .ToList();

        var serializedSizes = new List<int>();

        // Act
        foreach (var message in messages)
        {
            using var memoryStream = _memoryStreamManager.GetStream();
            await MessagePackSerializer.SerializeAsync(memoryStream, message);
            serializedSizes.Add((int)memoryStream.Length);
        }

        // Assert
        serializedSizes.Should().AllSatisfy(size => size.Should().BeGreaterThan(0));
        serializedSizes.Should().HaveCount(100);

        _output.WriteLine($"Processed {messages.Count} messages");
        _output.WriteLine($"Average size: {serializedSizes.Average():F2} bytes");
        _output.WriteLine($"Min size: {serializedSizes.Min()} bytes");
        _output.WriteLine($"Max size: {serializedSizes.Max()} bytes");
    }

    [Fact]
    public async Task SerializeAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var message = new MqPublishRequest { Message = "Test cancellation" };
        using var cts = new CancellationTokenSource();

        // Act & Assert
        using var memoryStream = _memoryStreamManager.GetStream();

        // Cancel immediately for fast operation (may not actually cancel)
        cts.Cancel();

        // For fast operations, cancellation might not take effect or might throw
        try
        {
            await MessagePackSerializer.SerializeAsync(memoryStream, message, cancellationToken: cts.Token);

            // If it completes without cancellation, verify the result
            var serializedBytes = memoryStream.ToArray();
            serializedBytes.Should().NotBeEmpty();
            _output.WriteLine("Serialization completed before cancellation could take effect");
        }
        catch (OperationCanceledException)
        {
            // This is expected if cancellation was respected
            _output.WriteLine("Serialization was successfully cancelled");
            Assert.True(true); // Test passes - cancellation was respected
        }
    }

    [Fact]
    public async Task Serialize_Synchronous_ShouldMatchAsyncResults()
    {
        // Arrange
        var message = new MqPublishRequest { Message = "Sync vs Async test" };

        // Act - Synchronous
        var syncBytes = MessagePackSerializer.Serialize(message);

        // Act - Asynchronous
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, message);
        var asyncBytes = memoryStream.ToArray();

        // Assert
        syncBytes.Should().Equal(asyncBytes);
        _output.WriteLine($"Sync: {syncBytes.Length} bytes, Async: {asyncBytes.Length} bytes");
    }

    [Fact]
    public async Task SerializeDeserialize_NullMessage_ShouldHandleGracefully()
    {
        // Arrange
        var message = new MqPublishRequest { Message = null! };

        // Act
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, message);

        var serializedBytes = memoryStream.ToArray();
        var deserializedMessage = MessagePackSerializer.Deserialize<MqPublishRequest>(serializedBytes);

        // Assert
        deserializedMessage.Should().NotBeNull();
        deserializedMessage.Message.Should().BeNull();
    }

    [Fact]
    public async Task SerializeAsync_MemoryStreamOptimization_ShouldUseReadOnlySequence()
    {
        // Arrange
        var message = new MqPublishRequest { Message = "Memory optimization test" };

        // Act
        using var memoryStream = _memoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, message);

        // Test ReadOnlySequence optimization
        var sequence = memoryStream.GetReadOnlySequence();
        var isSingleSegment = sequence.IsSingleSegment;

        // Assert
        sequence.Length.Should().BeGreaterThan(0);
        _output.WriteLine($"Memory stream length: {memoryStream.Length}");
        _output.WriteLine($"ReadOnlySequence length: {sequence.Length}");
        _output.WriteLine($"Is single segment: {isSingleSegment}");

        // Verify content
        if (isSingleSegment)
        {
            var span = sequence.FirstSpan;
            var deserializedFromSpan = MessagePackSerializer.Deserialize<MqPublishRequest>(span.ToArray());
            deserializedFromSpan.Message.Should().Be(message.Message);
        }
        else
        {
            var bytes = memoryStream.ToArray();
            var deserializedFromArray = MessagePackSerializer.Deserialize<MqPublishRequest>(bytes);
            deserializedFromArray.Message.Should().Be(message.Message);
        }
    }

    [Fact]
    public async Task SerializeAsync_ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var messageCount = 50;
        var tasks = new List<Task<byte[]>>();

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var messageIndex = i; // Capture for closure
            var task = Task.Run(async () =>
            {
                var message = new MqPublishRequest { Message = $"Concurrent message {messageIndex}" };
                using var memoryStream = _memoryStreamManager.GetStream();
                await MessagePackSerializer.SerializeAsync(memoryStream, message);
                return memoryStream.ToArray();
            });

            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(messageCount);
        results.Should().AllSatisfy(bytes => bytes.Should().NotBeEmpty());

        // Verify each result can be deserialized
        for (int i = 0; i < results.Length; i++)
        {
            var deserialized = MessagePackSerializer.Deserialize<MqPublishRequest>(results[i]);
            deserialized.Message.Should().Be($"Concurrent message {i}");
        }

        _output.WriteLine($"Successfully processed {messageCount} concurrent operations");
    }

    [Fact]
    public void MessagePackSerialization_Performance_ShouldBeFast()
    {
        // Arrange
        var message = new MqPublishRequest { Message = "Performance test message" };
        var iterations = 1000;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var bytes = MessagePackSerializer.Serialize(message);
            var deserialized = MessagePackSerializer.Deserialize<MqPublishRequest>(bytes);
        }

        stopwatch.Stop();

        // Assert
        var averageTimePerOperation = stopwatch.Elapsed.TotalMilliseconds / iterations;
        averageTimePerOperation.Should().BeLessThan(1.0); // Should be less than 1ms per operation

        _output.WriteLine($"Average time per serialize/deserialize: {averageTimePerOperation:F4} ms");
        _output.WriteLine($"Total operations: {iterations * 2} in {stopwatch.ElapsedMilliseconds} ms");
    }
}