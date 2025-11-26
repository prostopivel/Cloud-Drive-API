using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace ApiGateway.Transforms
{
    public class CustomTransform : ITransformProvider
    {
        public void Apply(TransformBuilderContext context)
        {
            context.AddRequestTransform(transformContext =>
            {
                var path = transformContext.HttpContext.Request.Path;

                if (path.StartsWithSegments("/auth-service"))
                {
                    transformContext.HttpContext.Request.Path = path.Value?.Replace("/auth-service", "");
                    transformContext.ProxyRequest.RequestUri = new Uri(
                        $"http://auth-service:5001{transformContext.HttpContext.Request.Path}{transformContext.HttpContext.Request.QueryString}");
                }
                else if (path.StartsWithSegments("/filemetadata-service"))
                {
                    transformContext.HttpContext.Request.Path = path.Value?.Replace("/filemetadata-service", "");
                    transformContext.ProxyRequest.RequestUri = new Uri(
                        $"http://filemetadata-service:5002{transformContext.HttpContext.Request.Path}{transformContext.HttpContext.Request.QueryString}");
                }
                else if (path.StartsWithSegments("/filestorage-service"))
                {
                    transformContext.HttpContext.Request.Path = path.Value?.Replace("/filestorage-service", "");
                    transformContext.ProxyRequest.RequestUri = new Uri(
                        $"http://filestorage-service:5003{transformContext.HttpContext.Request.Path}{transformContext.HttpContext.Request.QueryString}");
                }

                return ValueTask.CompletedTask;
            });
        }

        public void ValidateCluster(TransformClusterValidationContext context)
        {
        }

        public void ValidateRoute(TransformRouteValidationContext context)
        {
        }
    }
}
