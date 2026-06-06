using Demo.Application.Exceptions;
using Demo.Application.Utils;
using Demo.Domain;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace Demo.Application.Account;

public sealed record AccountLoginCommand(string Email, string Password) : ICommand<LoginResponse>;

public class AccountLoginHandler : ICommandHandler<AccountLoginCommand, LoginResponse>
{
    private readonly IUserService _userService;
    private readonly HybridCache _cache;

    public AccountLoginHandler(IUserService userService, HybridCache cache)
    {
        _userService = userService;
        _cache = cache;
    }
    
    public async Task<LoginResponse> HandleAsync(AccountLoginCommand command, CancellationToken ct)
    {
        var user = await _cache.GetOrCreateAsync<User?>(
            CacheKeys.UserInfoKey(command.Email),
            async (cancellationToken) =>
            {
                var user = await _userService.GetUserAsync(command.Email, cancellationToken);
                return user;
            },
            new HybridCacheEntryOptions()
            {
                Expiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: ct);
        
        if (user is null)
            throw new UserNotFoundException($"User Not Found. {command.Email}");

        bool checkPassword = StringUtil.CheckPassword(user.Salt, command.Password, user.Password);
        if (!checkPassword)
        {
            throw new UnauthorizedAccessException("Invalid password.");
        }

        // Assuming a successful login, you would typically return a response.
        // For demonstration purposes, we'll return a basic LoginResponse.
        return new LoginResponse();
    }
}
