namespace Shared.Messaging.Interfaces
{
    public interface IMessageConsumer
    {
        void StartConsuming();
        void StopConsuming();
    }
}
