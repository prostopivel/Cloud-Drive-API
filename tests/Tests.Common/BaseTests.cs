using Xunit;

namespace Tests.Common
{
    public abstract class BaseTests<TFactory, T>
        : IClassFixture<TFactory>
        where TFactory : BaseApiFactory<T> where T : class
    {
        protected readonly TFactory _factory;

        protected BaseTests(TFactory factory)
        {
            _factory = factory;
        }
    }
}
