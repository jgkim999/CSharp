﻿using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace RabbitMq.Common;

public abstract class ProducerBase<T> : RabbitMqClientBase, IRabbitMqProducer<T>
{
    private readonly ILogger<ProducerBase<T>> _logger;

    protected ProducerBase(
        ConnectionFactory connectionFactory,
        ILogger<RabbitMqClientBase> logger,
        ILogger<ProducerBase<T>> producerBaseLogger) :
        base(connectionFactory, logger)
    {
        _logger = producerBaseLogger;
    }

    protected abstract string ExchangeName { get; }
    protected abstract string RoutingKeyName { get; }
    protected abstract string AppId { get; }

    public virtual void Publish(T @event)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));
            var properties = Channel.CreateBasicProperties();
            properties.AppId = AppId;
            properties.ContentType = "application/json";
            properties.DeliveryMode = 1; // Doesn't persist to disk
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            Channel.BasicPublish(ExchangeName, RoutingKeyName, body: body, basicProperties: properties);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while publishing");
        }
    }
}
