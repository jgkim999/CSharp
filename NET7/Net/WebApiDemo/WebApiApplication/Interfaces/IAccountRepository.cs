using WebApiDomain.Models;

namespace WebApiApplication.Interfaces;

public interface IAccountRepository
{
    Task<bool> AddAsync(string name);
    Task<Account> GetAsync(string name);
}
