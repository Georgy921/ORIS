using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HttPServer.Core.Attributes;
using HttPServer.Suka;

namespace HttPServer.Endpoints
{
    [Endpoint]
    public class BonxEndpoint
    {
        [HttpGet("/bonx/")]
        public void MainPage(HttpListenerContext context)
        {
            Global.Server.SendStaticResponse(context, HttpStatusCode.OK, Global.Settings.Model.StaticDirectoryPath + "/bonx/index.html");
        }
    }
}
