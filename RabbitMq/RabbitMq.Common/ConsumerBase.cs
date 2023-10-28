using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMq.Common;

public abstract class ConsumerBase : RabbitMqClientBase
{
    private readonly ILogger<ConsumerBase> _logger;
    private readonly IMediator _mediator;

    public ConsumerBase(
        IMediator mediator,
        ConnectionFactory connectionFactory,
        ILogger<ConsumerBase> consumerLogger,
        ILogger<RabbitMqClientBase> logger) :
        base(connectionFactory, logger)
    {
        _mediator = mediator;
        _logger = consumerLogger;
    }

    protected abstract string QueueName { get; }

    protected virtual async Task OnEventReceived<T>(object sender, BasicDeliverEventArgs @event)
    {
        try
        {
            var body = Encoding.UTF8.GetString(@event.Body.ToArray());
            var message = JsonConvert.DeserializeObject<T>(body);

            await _mediator.Send(message);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while retrieving message from queue.");
        }
        finally
        {
            Channel.BasicAck(@event.DeliveryTag, false);
        }
    }
}
