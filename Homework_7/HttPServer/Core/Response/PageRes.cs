using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HttPServer.Suka;
using TemplateEngine;

namespace HttPServer.Framework.Core.Response
{
    public class PageRes : IResponse
    {
        private string _templatePath;
        private object _data;

        public HttpStatusCode StatusCode { get; }

        public PageRes(string templatePath, object data, HttpStatusCode statusCode)
        {
            _templatePath = Global.Settings.Model.StaticDirectoryPath + templatePath;
            _data = data;
            StatusCode = statusCode;
        }

        public string Execute(HttpListenerContext httpContext)
        {
            var templateRenderer = new HtmlTemplateRenderer();

            var renderedPage = templateRenderer.RenderFromFile(_templatePath, _data);

            return renderedPage;
        }

    }
}
