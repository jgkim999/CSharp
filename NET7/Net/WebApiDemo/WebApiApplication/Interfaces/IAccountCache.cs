using WebApiDomain.Models;

namespace WebApiApplication.Interfaces;

public interface IAccountCache
{
    Task SetAsync(Account dto);
}
