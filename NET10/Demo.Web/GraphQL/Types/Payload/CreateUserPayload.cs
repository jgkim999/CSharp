using Demo.Application.DTO.User;

namespace Demo.Web.GraphQL.Types.Payload;

public record CreateUserPayload(UserDto? User, IList<string>? Errors = null);