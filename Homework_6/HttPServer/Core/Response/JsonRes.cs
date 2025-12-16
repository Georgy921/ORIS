using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttPServer.Framework.Core.Response
{
    public class JsonRes : IResponse
    {
        private readonly string _jsonString;
        public HttpStatusCode StatusCode { get; }

        public JsonRes(string jsonString, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _jsonString = jsonString;
            StatusCode = statusCode;
        }

        public string Execute(HttpListenerContext httpContext) => _jsonString;
    }
}
