using Demo.Application.Account;
using Demo.Domain;
using FastEndpoints;
using LiteBus.Commands.Abstractions;

namespace WebApiService.Endpoints.Account.Create;

public class AccountCreateEndpoint : Endpoint<AccountCreateRequest, AccountCreateResponse, Mapper>
{
    private readonly ICommandMediator _command;

    public AccountCreateEndpoint(ICommandMediator command)
    {
        _command = command;
    }

    public override void Configure()
    {
        Post("/account/create");
        AllowAnonymous();
    }

    public override async Task<AccountCreateResponse> HandleAsync(AccountCreateRequest req, CancellationToken ct)
    {
        var command = new AccountCreateCommand(req.Email, req.Password);
        var response = await _command.SendAsync(command, ct);
        return response;
    }
}
