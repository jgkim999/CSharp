namespace Demo.Web.GraphQL.Types.Payload;

public record TestLoggingPayload(
    string? Message,
    string? TraceId,
    string? SpanId,
    string? Timestamp,
    IList<string>? Errors = null);
