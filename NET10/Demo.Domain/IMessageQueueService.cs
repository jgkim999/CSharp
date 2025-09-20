using Demo.Domain.Enums;

namespace Demo.Domain;

public interface IMqPublishService
{
    ValueTask PublishMultiAsync(string message, string? correlationId = null);
    ValueTask PublishUniqueAsync(string target, string message, string? correlationId = null);
    ValueTask PublishAnyAsync(string message, string? correlationId = null);
}

public interface IMqConsumerService
{
    ValueTask ConsumeMessagesAsync();
}
