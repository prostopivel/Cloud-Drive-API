using Microsoft.Extensions.Logging;
using Shared.Messaging.Interfaces;
using System.Text.Json;

namespace Tests.Common.Mocks
{
    public class MockMessageBus : IMessageBus
    {
        private ILogger<MockMessageBus>? _logger;

        public class PublishedMessage
        {
            public string ExchangeName { get; set; } = string.Empty;
            public string MessageType { get; set; } = string.Empty;
            public string MessageBody { get; set; } = string.Empty;
            public object OriginalMessage { get; set; } = null!;
            public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        }

        public class Subscription
        {
            public string ExchangeName { get; set; } = string.Empty;
            public string QueueName { get; set; } = string.Empty;
            public Delegate Handler { get; set; } = null!;
            public Type MessageType { get; set; } = null!;
        }

        public List<PublishedMessage> PublishedMessages { get; } = [];
        public List<Subscription> Subscriptions { get; } = [];

        public void Publish<T>(T message, string exchangeName)
            where T : class
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            PublishedMessages.Add(new PublishedMessage
            {
                ExchangeName = exchangeName,
                MessageType = typeof(T).Name,
                MessageBody = jsonMessage,
                OriginalMessage = message
            });

            _logger?.LogDebug("Mock: Published message to exchange {ExchangeName}: {MessageType}",
                exchangeName, typeof(T).Name);

            // Trigger subscriptions for this exchange
            var matchingSubscriptions = Subscriptions
                .Where(s => s.ExchangeName == exchangeName && s.MessageType == typeof(T))
                .ToList();

            foreach (var subscription in matchingSubscriptions)
            {
                try
                {
                    subscription.Handler.DynamicInvoke(message);
                    _logger?.LogDebug("Mock: Delivered message to subscription {QueueName}",
                        subscription.QueueName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Mock: Error delivering message to subscription {QueueName}",
                        subscription.QueueName);
                }
            }
        }

        public void Subscribe<T>(string exchangeName, string queueName, Action<T> handler)
            where T : class
        {
            Subscriptions.Add(new Subscription
            {
                ExchangeName = exchangeName,
                QueueName = queueName,
                Handler = handler,
                MessageType = typeof(T)
            });

            _logger?.LogInformation("Mock: Subscribed to exchange {ExchangeName} with queue {QueueName} for {MessageType}",
                exchangeName, queueName, typeof(T).Name);
        }

        public void Clear()
        {
            PublishedMessages.Clear();
            Subscriptions.Clear();
            _logger?.LogDebug("Mock: Cleared all messages and subscriptions");
        }

        public List<T> GetPublishedMessages<T>(string? exchangeName = null)
            where T : class
        {
            var query = PublishedMessages.AsEnumerable();

            if (!string.IsNullOrEmpty(exchangeName))
            {
                query = query.Where(m => m.ExchangeName == exchangeName);
            }

            return [.. query
                .Where(m => m.OriginalMessage is T)
                .Select(m => (T)m.OriginalMessage)];
        }

        public void SetLogger(ILogger<MockMessageBus> logger)
        {
            _logger = logger;
        }

        public bool HasPublishedMessage<T>(Func<T, bool> predicate, string? exchangeName = null)
            where T : class
        {
            return GetPublishedMessages<T>(exchangeName).Any(predicate);
        }

        public int GetPublishedMessageCount<T>(string? exchangeName = null)
            where T : class
        {
            return GetPublishedMessages<T>(exchangeName).Count;
        }

        public void SimulateMessage<T>(T message, string exchangeName)
            where T : class
        {
            Publish(message, exchangeName);
        }
    }
}
