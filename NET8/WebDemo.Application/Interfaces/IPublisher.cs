namespace WebDemo.Application.Interfaces;
public interface IPublisher
{
    Task PublishAsync(string message);
}
