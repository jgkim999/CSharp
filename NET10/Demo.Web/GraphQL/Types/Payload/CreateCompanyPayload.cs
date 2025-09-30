namespace Demo.Web.GraphQL.Types.Payload;

public record CreateCompanyPayload(bool Success, string? Message, IList<string>? Errors = null);
