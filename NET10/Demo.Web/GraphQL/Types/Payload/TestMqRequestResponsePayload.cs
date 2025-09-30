namespace Demo.Web.GraphQL.Types.Payload;

public record TestMqRequestResponsePayload(
    bool Success,
    string? RequestId,
    string? ResponseId,
    string? Target,
    double ProcessingTime,
    string? Timestamp,
    IList<string>? Errors = null);
