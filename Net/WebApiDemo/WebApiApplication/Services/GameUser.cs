using WebApiApplication.Interfaces;

namespace WebApiApplication.Services;

public class GameUser : IGameUser
{
    public long AccountId { get; set; }

    public GameUser(long accountId)
    {
        AccountId = accountId;
    }
}
