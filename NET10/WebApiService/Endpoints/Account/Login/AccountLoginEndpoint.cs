using Demo.Application.Account;
using Demo.Domain;
using FastEndpoints;
using LiteBus.Commands.Abstractions;

namespace WebApiService.Endpoints.Account.Login;

public sealed class AccountLoginEndpoint : Endpoint<LoginRequest, LoginResponse, Mapper>
{
    private readonly ICommandMediator _command;

    public AccountLoginEndpoint(ICommandMediator command)
    {
        _command = command;
    }

    public override void Configure()
    {
        Post("/account/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest r, CancellationToken ct)
    {
        var result = await _command.SendAsync(new AccountLoginCommand(r.Email, r.Password), ct);
        await Send.ResponseAsync(result, cancellation: ct);
    }
}
