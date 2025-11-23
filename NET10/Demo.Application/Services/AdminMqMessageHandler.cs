using Demo.Domain;
using Demo.Domain.Enums;

namespace Demo.Application.Services;

public class AdminMqMessageHandler : IMqMessageHandler
{
    public ValueTask<string?> HandleAsync(MqSenderType senderType, string? sender, string? correlationId, string? messageId, string message,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public ValueTask<object?> HandleBinaryMessageAsync(MqSenderType senderType, string? sender, string? correlationId, string? messageId,
        object messageObject, Type messageType, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
