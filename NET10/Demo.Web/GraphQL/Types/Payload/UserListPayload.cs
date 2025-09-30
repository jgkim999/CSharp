using Demo.Application.DTO.User;

namespace Demo.Web.GraphQL.Types.Payload;

public record UserListPayload(List<UserDto>? Items, int TotalItems, IList<string>? Errors = null);
