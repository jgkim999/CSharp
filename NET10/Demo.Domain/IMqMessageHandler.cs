using Demo.Domain.Enums;

namespace Demo.Domain;

public interface IMqMessageHandler
{
    ValueTask HandleAsync(
        MqSenderType senderType,
        string? sender,
        string? correlationId,
        string? messageId,
        string message,
        CancellationToken ct);
}
