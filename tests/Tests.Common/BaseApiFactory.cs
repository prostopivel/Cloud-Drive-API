using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Common
{
    public abstract class BaseApiFactory<T> : WebApplicationFactory<T>, IAsyncLifetime
        where T : class
    {
        protected Dictionary<string, IContainer> Containers { get; init; }

        protected BaseApiFactory()
        {
            Containers = [];
        }

        public HttpClient CreateClientWithUserId(string? userId = null)
        {
            var client = CreateClient();
            if (userId != null)
            {
                client.DefaultRequestHeaders.Add("userId", userId);
            }
            return client;
        }

        public abstract Task InitializeAsync();
        public abstract new Task DisposeAsync();
        protected abstract bool CanConnectToExistingContainers();
    }
}