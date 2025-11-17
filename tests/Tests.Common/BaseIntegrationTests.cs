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
        protected readonly Func<Task> _resetState;

        protected BaseIntegrationTests(TFactory factory)
            : base(factory)
        {
            _client = factory.CreateClient();
            _resetState = factory.ResetAsync;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public abstract Task DisposeAsync();

        public abstract Task InitializeAsync();
    }
}
