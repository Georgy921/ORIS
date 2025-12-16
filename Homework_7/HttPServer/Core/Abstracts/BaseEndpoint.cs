using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HttPServer.Framework.Core.Response;

namespace HttPServer.Framework.Core.Abstracts
{
    public abstract class BaseEndpoint
    {
        protected HttpListenerContext HttpContext { get; set; }

        public void SetContext(HttpListenerContext httpContext)
        {
            HttpContext = httpContext;
        }

        protected IResponse Page(string templatePath, object data, HttpStatusCode statusCode = HttpStatusCode.OK)
            => new PageRes(templatePath, data, statusCode);
        protected IResponse Json(string jsonString, HttpStatusCode statusCode = HttpStatusCode.OK)
            => new JsonRes(jsonString, statusCode);
    }
}
