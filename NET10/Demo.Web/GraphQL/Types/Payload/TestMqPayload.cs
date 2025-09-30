namespace Demo.Web.GraphQL.Types.Payload;

public record TestMqPayload(
    string? Message,
    string? TraceId,
    string? SpanId,
    string? Timestamp,
    IList<string>? Errors = null);
