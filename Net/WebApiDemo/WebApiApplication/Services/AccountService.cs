using Microsoft.Extensions.Logging;
using WebApiApplication.Exceptions;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiApplication.Services;

public class AccountService : IAccountService
{
    private readonly ILogger<AccountService> _logger;
    private readonly IAccountRepository _accountRepo;

    public AccountService(ILogger<AccountService> logger, IAccountRepository accountRepo)
    {
        _logger = logger;
        _accountRepo = accountRepo;
    }

    public async Task<AccountDto> LoginAsync(LoginReq req)
    {
        Account account = await _accountRepo.GetAsync(req.Name);
        if (account == null)
        {
            bool success = await _accountRepo.AddAsync(req.Name);
            if (success)
            {
                account = await _accountRepo.GetAsync(req.Name);
            }
            else
            {
                throw new CreateAccountFailedException();
            }
        }
        AccountDto dto = new AccountDto()
        {
            Id = account.Id,
            Name = account.Name,
            Ulid = Ulid.NewUlid().ToString(),
        };
        return dto;
    }
}
