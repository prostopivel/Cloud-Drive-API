using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Common.Models;
using System.Text;
using System.Text.Json;

namespace Shared.Messaging
{
    public class RabbitMQMessageBus : IMessageBus, IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQMessageBus> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private bool _disposed = false;

        public RabbitMQMessageBus(IOptions<RabbitMQSettings> settings,
            ILogger<RabbitMQMessageBus> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            InitializeConnection();
        }

        private void InitializeConnection()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare the exchange that will be used for file events
                _channel.ExchangeDeclare(exchange: "file_events", type: ExchangeType.Fanout, durable: true);

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
                throw;
            }
        }

        public void Publish<T>(T message, string exchangeName) where T : class
        {
            if (_channel == null || _connection?.IsOpen != true)
            {
                throw new InvalidOperationException("RabbitMQ connection is not available");
            }

            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                _channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: string.Empty,
                    basicProperties: null,
                    body: body);

                _logger.LogDebug("Message published to exchange {ExchangeName}: {MessageType}",
                    exchangeName, typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to exchange {ExchangeName}", exchangeName);
                throw;
            }
        }

        public void Subscribe<T>(string exchangeName, string queueName, Action<T> handler) where T : class
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("RabbitMQ channel is not available");
            }

            try
            {
                // Declare queue and bind to exchange
                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var deserializedMessage = JsonSerializer.Deserialize<T>(message);

                        if (deserializedMessage != null)
                        {
                            handler(deserializedMessage);
                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize message for queue {QueueName}", queueName);
                            _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message in queue {QueueName}", queueName);
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                _logger.LogInformation("Subscribed to queue {QueueName} on exchange {ExchangeName}",
                    queueName, exchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to exchange {ExchangeName}", exchangeName);
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel?.Close();
                _connection?.Close();
                _disposed = true;
                _logger.LogInformation("RabbitMQ message bus disposed");
            }
        }
    }
}
