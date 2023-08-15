using WebApiDomain.Models;

namespace WebApiApplication.Interfaces;

public interface IAccountCache
{
    Task SetAsync(AccountDto dto);
}
