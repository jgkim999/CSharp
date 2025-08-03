using Demo.Application.Commands;
using FastEndpoints;
using LiteBus.Commands.Abstractions;

namespace Demo.Web.Endpoints.User;

public class UserCreateEndpointV1 : Endpoint<UserCreateRequest>
{
    private readonly ICommandMediator _commandMediator;
    
    public UserCreateEndpointV1(ICommandMediator commandMediator)
    {
        _commandMediator = commandMediator;
    }
    
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UserCreateRequest req, CancellationToken ct)
    {
        await _commandMediator.SendAsync(
            new UserCreateCommand(req.Name, req.Email, req.PasswordSha256),
            cancellationToken: ct);
    }
}
