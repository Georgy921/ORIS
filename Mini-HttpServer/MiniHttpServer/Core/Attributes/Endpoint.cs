namespace MiniHttpServer.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EndpointAttribute : Attribute
    {
        public EndpointAttribute() { }
    }
}