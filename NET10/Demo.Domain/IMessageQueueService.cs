namespace Demo.Domain;

public interface IMqPublishService
{
    ValueTask PublishMessageAsync(string message);
}

public interface IMqConsumerService
{
    ValueTask ConsumeMessagesAsync();
}
