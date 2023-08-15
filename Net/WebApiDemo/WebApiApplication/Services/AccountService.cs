using Microsoft.Extensions.Logging;
using WebApiApplication.Exceptions;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;
using WebApiDomain.Utils;

namespace WebApiApplication.Services;

public class AccountService : IAccountService
{
    private readonly ILogger<AccountService> _logger;
    private readonly IAccountRepository _accountRepo;
    private readonly IAccountCache _accountCache;
    private readonly ISessionService _sessionService;

    public AccountService(
        ILogger<AccountService> logger,
        IAccountRepository accountRepo,
        IAccountCache accountCache,
        ISessionService sessionService)
    {
        _logger = logger;
        _accountRepo = accountRepo;
        _accountCache = accountCache;
        _sessionService = sessionService;
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

        var sessionId = SessionIdGenerator.GetId(account.Id);

        AccountDto dto = new AccountDto()
        {
            SessionId = sessionId,
            Name = account.Name,
            //Ulid = Ulid.NewUlid().ToString(),
        };
        await _sessionService.SetAsync(account.Id, sessionId);
        await _accountCache.SetAsync(account);
        return dto;
    }
}
