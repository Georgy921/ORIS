using System;
using System.Net;
using HttPServer.Core.Attributes;
using HttPServer.Suka;

[Endpoint]
public class BonxEndpoint
{
    [HttpGet("/bonx/")]
    public void MainPage(HttpListenerContext context)
    {
        Global.Server.SendStaticResponse(context, HttpStatusCode.OK, Global.Settings.Model.StaticDirectoryPath + "/bonx/index.html");
    }
}