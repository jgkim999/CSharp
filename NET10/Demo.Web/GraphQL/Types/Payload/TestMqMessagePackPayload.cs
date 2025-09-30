namespace Demo.Web.GraphQL.Types.Payload;

public record TestMqMessagePackPayload(
    string? Message,
    string? MessageType,
    string? TraceId,
    string? SpanId,
    string? Timestamp,
    IList<string>? Errors = null);
