using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InvoiceProcessorLogic;

public class HttpServer
{
    private HttpListener _listener;
    private bool _isRunning;
    private string _prefix;
    private SettingsManager _settingsManager;

    public HttpServer(SettingsManager settingsManager, string prefix = "http://localhost:8080/")
    {
        _settingsManager = settingsManager;
        _prefix = prefix;
        _listener = new HttpListener();
        _listener.Prefixes.Add(_prefix);
    }

    public void Start()
    {
        _listener.Start();
        _isRunning = true;
        Console.WriteLine("Сервер запущен на " + _prefix);

        while (_isRunning)
        {
            try
            {
                var context = _listener.GetContext();
                ProcessRequest(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        Console.WriteLine("Сервер остановлен");
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string path = request.Url.AbsolutePath;
        string method = request.HttpMethod;

        Console.WriteLine($"{method} {path}");

        if (method == "GET" && path == "/health")
        {
            response.StatusCode = 200;
            SendResponse(response, "OK");
        }
        else if (method == "GET" && path == "/config")
        {
            response.StatusCode = 200;
            SendResponse(response, _settingsManager.Get().ToString());
        }
        else if (method == "GET" && path == "/config/reload")
        {
            response.StatusCode = 200;
            _settingsManager = new SettingsManager("config.json");
            SendResponse(response, "Успешно обновлены настройки");
        }
        else
        {
            response.StatusCode = 404;
            SendResponse(response, "Not Found");
        }
    }

    private void SendResponse(HttpListenerResponse response, string text)
    {
        response.ContentType = "text/plain; charset=utf-8";
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

}