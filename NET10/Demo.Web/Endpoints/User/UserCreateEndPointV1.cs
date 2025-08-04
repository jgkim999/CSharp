using Demo.Application.Commands;
using FastEndpoints;
using LiteBus.Commands.Abstractions;

namespace Demo.Web.Endpoints.User;

public class UserCreateEndpointV1 : Endpoint<UserCreateRequest, EmptyResponse>
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
        var ret = await _commandMediator.SendAsync(
            new UserCreateCommand(req.Name, req.Email, req.Password),
            cancellationToken: ct);
        if (ret.Result.IsFailed)
        {
            foreach (var error in ret.Result.Errors)
            {
                AddError(error.Message);
            }
            await Send.ErrorsAsync(500, ct);
        }
        else
        {
            await Send.OkAsync(cancellation: ct);
        }
        //ThrowIfAnyErrors();
    }
}
