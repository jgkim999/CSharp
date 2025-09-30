namespace Demo.Web.GraphQL.Types.Payload;

public record ServerTimePayload(
    string? Utc,
    string? Korea,
    string? KoreanCalendar,
    IList<string>? Errors = null);
