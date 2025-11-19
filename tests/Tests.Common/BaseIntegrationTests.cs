using System.Text.Json;
using Xunit;

namespace Tests.Common
{
    public abstract class BaseIntegrationTests<TFactory, T>
        : BaseTests<TFactory, T>, IAsyncLifetime
        where TFactory : BaseApiFactory<T> where T : class
    {
        protected readonly HttpClient _client;
        protected readonly JsonSerializerOptions _jsonOptions;

        protected BaseIntegrationTests(TFactory factory,
            string? userId = null)
            : base(factory)
        {
            _client = factory.CreateClientWithUserId(userId);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public abstract Task DisposeAsync();
        public abstract Task InitializeAsync();
    }
}
