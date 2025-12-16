using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json.Nodes;
using HttPServer.Core.Handler;
using HttPServer.Core.Handlers;
using HttPServer.Suka;
using HttPServer.Utils;
using MiniHttpServer.Utils;

namespace HttpServer;

public class HTTPServer
{
    private readonly HttpListener _listener = new();

    public void Start()
    {
        _listener.Prefixes.Add($"http://{Global.Settings.Model.Domain}:{Global.Settings.Model.Port}/");
        _listener.Start();
        Global.Logger.ServerStarted(Global.Settings.Model.Domain, Global.Settings.Model.Port);
        Receive();
        Global.Endpoints = EndpointsRegistry.LoadEndpoints(Assembly.GetExecutingAssembly());
    }

    public void Stop()
    {
        _listener.Stop();
    }

    private void Receive()
    {
        _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
    }

    private void ListenerCallback(IAsyncResult result)
    {

        if (!_listener.IsListening) return;
        var context = _listener.EndGetContext(result);

        var staticFilesHandler = new StaticFilesHandler();
        var controllerHandler = new EndpointsHandler();
        staticFilesHandler.Successor = controllerHandler;
        staticFilesHandler.HandleRequest(context);

        Receive();
    }

    public void SendStaticResponse(HttpListenerContext context, HttpStatusCode statusCode, string path)
    {
        var response = context.Response;
        var request = context.Request;

        response.StatusCode = (int)statusCode;
        response.ContentType = ContentTypes.GetContentTypeFromFile(path);

        var buffer = HttPServer.Utils.Buffer.GetBytesFromFile(path);
        response.ContentLength64 = buffer.Length;

        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);


        if (response.StatusCode == 200)
            Global.Logger.LogMess($"Загружен {request.Url.AbsolutePath} {request.HttpMethod} - {response.StatusCode}");
        else
            Global.Logger.LogMess($"Ошибка: {request.Url.AbsolutePath} {request.HttpMethod} - {response.StatusCode}");
    }

    public void SendJsonResponse(HttpListenerContext context, HttpStatusCode statusCode, string jsonString = "")
    {
        var response = context.Response;
        var request = context.Request;

        response.StatusCode = (int)statusCode;
        response.ContentType = ContentTypes.GetContentTypeByExtension(".json");

        var buffer = HttPServer.Utils.Buffer.GetBytesFromJson(jsonString);
        response.ContentLength64 = buffer.Length;

        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);


        if (response.StatusCode == 200)
            Global.Logger.LogMess($"обработан: {request.Url.AbsolutePath} {request.HttpMethod} - {response.StatusCode}");
        else
            Global.Logger.LogMess($"Ошибка: {request.Url.AbsolutePath} {request.HttpMethod} - {response.StatusCode}");
    }

    public void Send404Response(HttpListenerContext context, string path)
    {
        try
        {
            var path404 = Global.Settings.Model.StaticDirectoryPath + path.Split('/')[1] + "/404.html";

            if (File.Exists(path404))
            {
                Global.Server.SendStaticResponse(context, HttpStatusCode.NotFound, path404);
            }
            else
            {
                Global.Server.SendStaticResponse(context, HttpStatusCode.NotFound,
                    Global.Settings.Model.StaticDirectoryPath + "/404.html");
            }
        }
        catch (Exception ex)
        {
            Global.Server.SendStaticResponse(context, HttpStatusCode.NotFound,
                Global.Settings.Model.StaticDirectoryPath + "/404.html");
            Global.Logger.LogMess($"{ex}");
        }
        finally
        {
            if (context.Response.StatusCode == 200)
                Global.Logger.LogMess($"обработан: {context.Request.Url.AbsolutePath} {context.Request.HttpMethod} - {context.Response.StatusCode}");
            else
                Global.Logger.LogMess($"Ошибка: {context.Request.Url.AbsolutePath} {context.Request.HttpMethod} - {context.Response.StatusCode}");

        }
    }
}