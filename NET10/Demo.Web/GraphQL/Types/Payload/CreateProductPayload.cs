namespace Demo.Web.GraphQL.Types.Payload;

public record CreateProductPayload(bool Success, string? Message, IList<string>? Errors = null);
