using System;

namespace MiniHttpServer.Utils;

public class Logger
{
    private static Logger _instance;
    private static object _lock = new();

    private Logger()
    {
    }

    public static Logger Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new Logger();
                }
            }
            return _instance;
        }
    }

    public void ServerStarted()
    {
        Console.WriteLine("Server is started");
    }
    public void ServerWaiting()
    {
        Console.WriteLine("Server is waiting");
    }

    public void ServerStopped()
    {
        var now = DateTime.Now;

        Console.WriteLine($"{now.ToString("dd.MM.yyyy hh:mm:ss")}: Сервер завершил работу");
    }

    public void ServerStarted(string domain, string port)
    {
        var now = DateTime.Now;

        Console.WriteLine($"Сервер был запущен {now.ToString("dd.MM.yyyy hh:mm:ss")}: Сервер работает на: http://{domain}:{port}/");
    }

    public void LogMess(string message)
    {
        var now = DateTime.Now;

        Console.WriteLine($"{now.ToString("hh:mm:ss")}: {message}");
    }
}