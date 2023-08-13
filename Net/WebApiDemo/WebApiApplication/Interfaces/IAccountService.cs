using WebApiDomain.Models;

namespace WebApiApplication.Interfaces;

public interface IAccountService
{
    Task<AccountDto> LoginAsync(LoginReq req);
}
