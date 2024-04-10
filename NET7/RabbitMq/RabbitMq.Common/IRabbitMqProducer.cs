namespace RabbitMq.Common;

public interface IRabbitMqProducer<in T>
{
    void Publish(T @event);
}
