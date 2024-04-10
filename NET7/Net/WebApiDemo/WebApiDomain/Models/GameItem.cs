namespace WebApiDomain.Models;

public class GameItem
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public int ItemId { get; set; }
    public long Amount { get; set; }
}
