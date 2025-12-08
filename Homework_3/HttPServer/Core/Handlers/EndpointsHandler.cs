using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Attributes;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace MiniHttpServer.Core.Handlers
{
    internal class EndpointsHandler : Handler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            if (true)
            {
                var request = context.Request;
                var endpointName = request.Url?.AbsolutePath.Split('/')[1]; ;

                var assembly = Assembly.GetExecutingAssembly();
                var endpont = assembly.GetTypes()
                                       .Where(t => t.GetCustomAttribute<EndpointAttribute>() != null)
                                       .FirstOrDefault(end => IsCheckedNameEndpoint(end.Name, endpointName));

                if (endpont == null) return; // TODO: 

                var method = endpont.GetMethods().Where(t => t.GetCustomAttributes(true)
                            .Any(attr => attr.GetType().Name.Equals($"Http{context.Request.HttpMethod}", 
                                                                    StringComparison.OrdinalIgnoreCase)))
                            .FirstOrDefault();

                if (method == null) return;  // TODO:            

                var ret = method.Invoke(Activator.CreateInstance(endpont), null);

            }
            // передача запроса дальше по цепи при наличии в ней обработчиков
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }

        private bool IsCheckedNameEndpoint(string endpointName, string className) =>
            endpointName.Equals(className, StringComparison.OrdinalIgnoreCase) ||
            endpointName.Equals($"{className}Endpoint", StringComparison.OrdinalIgnoreCase);


    }
}
