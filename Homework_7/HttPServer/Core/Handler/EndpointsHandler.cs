using System.Net;
using System.Reflection;
using HttPServer.Core.Abstracts;
using HttPServer.Suka;

namespace HttPServer.Core.Handlers
{
    public class EndpointsHandler : HttPServer.Core.Abstracts.Handler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            if (true)
            {
                var request = context.Request;
                var endpointPath = request.Url?.AbsolutePath;


                if (!Global.Endpoints.TryGetValue((endpointPath, request.HttpMethod), out var endpoint))
                {
                    Global.Server.Send404Response(context, endpointPath);
                    return;
                }

                if (endpoint.Item1 is not null)
                {
                    endpoint.Item2.Invoke(Activator.CreateInstance(endpoint.Item1), [context]);
                }
                else
                    Global.Server.Send404Response(context, endpointPath);

            }

            
        }
    }
}