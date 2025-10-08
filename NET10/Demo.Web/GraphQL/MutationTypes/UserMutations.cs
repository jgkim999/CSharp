using Demo.Application.DTO.User;
using Demo.Application.Handlers.Commands;
using Demo.Application.Services;
using Demo.Web.GraphQL.Types.Input;
using Demo.Web.GraphQL.Types.Payload;
using LiteBus.Commands.Abstractions;
using MapsterMapper;

namespace Demo.Web.GraphQL.MutationTypes;

public class UserMutations
{
    /*
    mutation {
        createUser(input: { name: "홍길동태권브이", email: "hong@example.com", password: "23232" }) {    
            user {
                name
                email
            },
            errors
        }
    }
    */
    public async Task<CreateUserPayload> CreateUserAsync(
        CreateUserInput input,
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        IMapper mapper,
        CancellationToken ct)
    {
        using var activity = telemetryService.StartActivity("user.mutations.create.user");

        UserCreateCommandResult ret = await commandMediator.SendAsync(
            new UserCreateCommand(input.Name, input.Email, input.Password),
            cancellationToken: ct);

        if (ret.Result.IsSuccess)
        {
            var createdUser = ret.Result.Value;
            var userDto = mapper.Map<UserDto>(createdUser);
            return new CreateUserPayload(userDto);
        }

        // 실패 시 에러와 함께 페이로드 반환
        var errors = ret.Result.Errors.Select(e => e.Message).ToList();
        return new CreateUserPayload(null, errors);
    }
}
