using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Common
{
    public abstract class BaseApiFactory<T> : WebApplicationFactory<T>, IAsyncLifetime
        where T : class
    {
        protected Dictionary<string, DockerContainer> Containers { get; init; }

        protected BaseApiFactory()
        {
            Containers = [];
        }

        public HttpClient CreateClientWithJwt(string token)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        public abstract Task InitializeAsync();
        public abstract new Task DisposeAsync();
        public abstract Task ResetAsync();
        protected abstract bool CanConnectToExistingContainers();
    }
}
