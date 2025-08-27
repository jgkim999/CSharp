namespace Demo.Domain;

public interface IMqPublishService
{
    /// <summary>
/// Asynchronously publishes the given text message to the message queue.
/// </summary>
/// <param name="message">The message payload to publish to the queue.</param>
/// <returns>A <see cref="ValueTask"/> that completes when the publish operation has finished.</returns>
ValueTask PublishMessageAsync(string message);
}

public interface IMqConsumerService
{
    /// <summary>
/// Asynchronously consumes messages from a message queue.
/// </summary>
/// <returns>A <see cref="ValueTask"/> representing the asynchronous consume operation.</returns>
ValueTask ConsumeMessagesAsync();
}
