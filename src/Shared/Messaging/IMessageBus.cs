namespace Shared.Messaging
{
    public interface IMessageBus
    {
        void Publish<T>(T message, string exchangeName) where T : class;
        void Subscribe<T>(string exchangeName, string queueName, Action<T> handler) where T : class;
    }
}
