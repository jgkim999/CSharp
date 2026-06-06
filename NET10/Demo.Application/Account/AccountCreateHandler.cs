using Bogus;
using Demo.Application.Exceptions;
using Demo.Domain;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using System.Security.Cryptography;
using System.Text;

namespace Demo.Application.Account;

public record AccountCreateCommand(string Email, string Password) : ICommand<AccountCreateResponse>;

public class AccountCreateHandler : ICommandHandler<AccountCreateCommand, AccountCreateResponse>
{
    private readonly IUserService _userService;
    private readonly HybridCache _cache;
    static Faker faker = new();

    public AccountCreateHandler(IUserService userService, HybridCache cache)
    {
        _userService = userService;
        _cache = cache;
    }

    public async Task<AccountCreateResponse> HandleAsync(AccountCreateCommand command, CancellationToken ct)
    {
        string salt = faker.Random.String2(16);
        using SHA256 sha256Hash = SHA256.Create();
        byte[] hashBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(command.Password + salt));
        string hashedPassword = BitConverter.ToString(hashBytes).Replace("-", "");
        
        User? user = await _userService.CreateUserAsync(command.Email, hashedPassword, salt, DateTime.Now, DateTime.Now, ct);
        if (user is null)
            throw new UserAlreadyExistException(command.Email);
        
        await _cache.SetAsync<User>(
            CacheKeys.UserInfoKey(command.Email),
            user,
            new HybridCacheEntryOptions()
            {
                Expiration = TimeSpan.FromHours(1)
            },
            cancellationToken: ct);
        return new AccountCreateResponse();
    }
}
