using System.Net;
using HttPServer.Suka;

namespace HttPServer.Core.Handler
{
    public class StaticFilesHandler : HttPServer.Core.Abstracts.Handler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var isGetMethod = request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
            var isStaticFile = request.Url.AbsolutePath.Split('\\').Any(x => x.Contains("."));

            if (isGetMethod && isStaticFile)
            {
                string path = request.Url.AbsolutePath.Trim('/');
                Global.Server.SendStaticResponse(context, HttpStatusCode.OK, path);
            }
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }
    }
}
