using FileMetadata.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Common.Models;
using Shared.Messaging.Events;
using System.Text;
using System.Text.Json;

namespace FileMetadata.Infrastructure.Services
{
    public class RabbitMQMessageConsumer : IMessageConsumer, IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMQSettings _rabbitMqSettings;
        private readonly ILogger<RabbitMQMessageConsumer> _logger;
        private IConnection? _connection;
        private RabbitMQ.Client.IModel? _channel;
        private const string ExchangeName = "file_events";
        private const string QueueName = "file_metadata_queue";

        public RabbitMQMessageConsumer(
            IServiceProvider serviceProvider,
            IOptions<RabbitMQSettings> rabbitMqSettings,
            ILogger<RabbitMQMessageConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _rabbitMqSettings = rabbitMqSettings.Value;
            _logger = logger;
        }

        public void StartConsuming()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _rabbitMqSettings.Host,
                    UserName = _rabbitMqSettings.Username,
                    Password = _rabbitMqSettings.Password,
                    Port = _rabbitMqSettings.Port
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare exchange and queue
                _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout, durable: true);
                _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: "");

                // Configure quality of service
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    try
                    {
                        await ProcessMessageAsync(message);
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message: {Message}", message);
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation("RabbitMQ consumer started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start RabbitMQ consumer");
                throw;
            }
        }

        public void StopConsuming()
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("RabbitMQ consumer stopped");
        }

        private async Task ProcessMessageAsync(string message)
        {
            try
            {
                var fileEvent = JsonSerializer.Deserialize<FileUploadedEvent>(message);
                if (fileEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize message: {Message}", message);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var fileMetadataService = scope.ServiceProvider.GetRequiredService<IFileMetadataService>();

                await fileMetadataService.CreateFileMetadataAsync(
                    fileEvent.FileId,
                    fileEvent.FileName,
                    fileEvent.OriginalName,
                    fileEvent.FileSize,
                    fileEvent.ContentType, //"application/octet-stream",
                    fileEvent.UserId,
                    fileEvent.StoragePath);

                _logger.LogInformation("Processed file upload event: {FileId} for user {UserId}",
                    fileEvent.FileId, fileEvent.UserId);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error for message: {Message}", message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload event: {Message}", message);
                throw;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartConsuming();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopConsuming();
            return Task.CompletedTask;
        }
    }
}
