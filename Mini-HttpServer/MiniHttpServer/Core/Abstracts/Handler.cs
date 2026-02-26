using System.Net;
using MiniHttpServer.Core.Attributes;

namespace MiniHttpServer.Core.Abstracts
{
    abstract class Handler
    {
        public Handler Successor { get; set; }
        public abstract void HandleRequest(HttpListenerContext context);
    }

}