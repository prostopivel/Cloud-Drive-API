using Microsoft.Extensions.Hosting;
using Shared.Messaging.Interfaces;

namespace Shared.Messaging
{
    public class MessageConsumerHostedService : IHostedService
    {
        private readonly IMessageConsumer _messageConsumer;

        public MessageConsumerHostedService(IMessageConsumer messageConsumer)
        {
            _messageConsumer = messageConsumer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _messageConsumer.StartConsuming();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _messageConsumer.StopConsuming();
            return Task.CompletedTask;
        }
    }
}
