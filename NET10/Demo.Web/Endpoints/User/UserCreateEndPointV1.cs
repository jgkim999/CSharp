using Demo.Application.Commands;
using FastEndpoints;
using LiteBus.Commands.Abstractions;

namespace Demo.Web.Endpoints.User;

public class UserCreateEndpointV1 : Endpoint<UserCreateRequest, EmptyResponse>
{
    private readonly ICommandMediator _commandMediator;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreateEndpointV1"/> class with the specified command mediator.
    /// </summary>
    public UserCreateEndpointV1(ICommandMediator commandMediator)
    {
        _commandMediator = commandMediator;
    }
    
    /// <summary>
    /// Configures the endpoint to handle HTTP POST requests at the route "/api/user/create" and allows anonymous access.
    /// </summary>
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();
    }

    /// <summary>
    /// Processes a user creation request by sending a command to create a new user and responds with the appropriate HTTP status based on the result.
    /// </summary>
    /// <param name="req">The user creation request containing name, email, and password.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
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
