namespace Demo.Domain;

public interface IMqPublishService
{
    ValueTask PublishMultiAsync(string message, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishMultiAsync(byte[] body, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishMultiAsync<T>(T messagePack, CancellationToken ct = default, string? correlationId = null) where T : class;
    
    ValueTask PublishUniqueAsync(string target, string message, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishUniqueAsync(string target, byte[] body, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishAnyAsync(string message, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishAnyAsync(byte[] body, CancellationToken ct = default, string? correlationId = null);
}
