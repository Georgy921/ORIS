namespace MiniHttpServer.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpPost : Attribute
    {
        public string? Route { get; }

        public HttpPost()
        {
        }

        public HttpPost(string? route)
        {
            Route = route;
        }
    }
}