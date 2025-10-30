using System;
using System.Net;
using System.Text;
using System.Threading;
using HttPServer.Shar;
using MiniHttpServer.Sharer;

namespace MiniHttpServer;

public class HttpServer
{
    private readonly HttpListener listener = new();
    private readonly Logger log = new();
    private readonly SettingsModel settings = SettingsModel.Instance;
    private Timer _waitingTimer;
    private const int WaitingDelayMs = 100;

    public HttpServer() { }

    public void Start()
    {
        listener.Prefixes.Add($"http://{settings.Domain}:{settings.Port}/");
        listener.Start();
        log.ServerStarted();
        log.ServerWaiting();
        Receive();
    }

    public void Stop()
    {
        listener.Stop();
        _waitingTimer?.Dispose();
    }

    private void Receive()
    {
        listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
    }

    private void ScheduleWaitingLog()
    {
        _waitingTimer?.Dispose();
        _waitingTimer = new Timer(_ =>
        {
            log.ServerWaiting();
        }, null, WaitingDelayMs, Timeout.Infinite);
    }

    private async void ListenerCallback(IAsyncResult result)
    {
        if (!listener.IsListening) return;

        var context = listener.EndGetContext(result);
        var response = context.Response;

        string requestedPath = context.Request.Url?.LocalPath ?? "/";
        string relativePath = requestedPath.TrimStart('/');

        if (string.IsNullOrEmpty(relativePath))
            relativePath = "index.html";

        var pathToFile = Path.Combine(settings.StaticDirectoryPath, relativePath);

        if (!File.Exists(pathToFile))
        {
            response.StatusCode = 404;
            var notFound = Encoding.UTF8.GetBytes("<h1>404 Not Found</h1>");
            response.ContentLength64 = notFound.Length;
            response.ContentType = "text/html";
            await response.OutputStream.WriteAsync(notFound);
            log.PrintMessage($"Файл не найден: {relativePath}");
            Receive();
            ScheduleWaitingLog();
            return;
        }

        var fileInfo = new FileInfo(pathToFile);
        var buffer = await File.ReadAllBytesAsync(pathToFile);
        response.ContentLength64 = buffer.Length;
        response.ContentType = ContentTypes.GetContentType(fileInfo.Extension);

        await using Stream output = response.OutputStream;
        if (buffer.Length > 0)
            await output.WriteAsync(buffer);
        await output.FlushAsync();

        log.PrintMessage($"Запрос обработан: {relativePath} ({fileInfo.Extension})");

        Receive();
        ScheduleWaitingLog();
    }
}