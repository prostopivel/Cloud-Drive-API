namespace FileMetadata.Core.Interfaces.Services
{
    public interface IMessageConsumer
    {
        void StartConsuming();
        void StopConsuming();
    }
}
