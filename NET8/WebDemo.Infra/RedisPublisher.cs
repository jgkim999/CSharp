using StackExchange.Redis;
using WebDemo.Application.Interfaces;
using WebDemo.Domain.Consts;

namespace WebDemo.Infra;

public class RedisPublisher : IPublisher
{
    private readonly ConnectionMultiplexer _multiplexer;
    
    public RedisPublisher(ConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }
    
    public async Task PublishAsync(string message)
    {
        await _multiplexer.GetSubscriber().PublishAsync(PubSubDefine.Channel, message);
    }
}
