using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Handlers;
using MiniHttpServer.Settings;
using MiniHttpServer.Shared;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace MiniHttpServer.Server 
{
    
    public class TaskAsyncResult : IAsyncResult
    {
        public Task? InnerTask { get; }

        public object? AsyncState { get; }

        public TaskAsyncResult(Task? task, object? state)
        {
            InnerTask = task;
            AsyncState = state;
        }
    
        public WaitHandle AsyncWaitHandle => ((IAsyncResult)InnerTask).AsyncWaitHandle;
        public bool CompletedSynchronously => ((IAsyncResult)InnerTask).CompletedSynchronously;
        public bool IsCompleted => ((IAsyncResult)InnerTask).IsCompleted;
    
    }
    
    public class HttpServer
    {
        public HttpListener _listener = new();
        private JsonEntity _config;
        private CancellationToken _token;

        public HttpServer(JsonEntity config) { _config = config; }

        public void Start(CancellationToken token)
        {
            _token = token;
            _listener = new HttpListener();
            string url = "http://" + _config.Domain + ":" + _config.Port + "/";
            _listener.Prefixes.Add(url);
            _listener.Start();
            Console.WriteLine("Сервер запущен! Проверяй в браузере: " + url);
            Receive();

            _token.Register(() =>
            {
                Console.WriteLine("Register /stop caught");
                Stop();
            });

            /*
            bool continues = true;
            var taskStop = Task.Run(() =>
            {
                while (continues)
                {
                    string? command = Console.ReadLine()?.Trim().ToLower();
                    if (command == "/stop")
                    {
                        Console.WriteLine("HttpServer /stop caught");
                        continues = false;
                        Stop();
                        Console.WriteLine($"Server is listening: {_listener.IsListening}");
                        break;
                    }
                }
            });

            await taskStop;
            */
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private void Receive()
        {
            BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        }

        protected async void ListenerCallback(IAsyncResult result)
        {
            if (_listener.IsListening && !_token.IsCancellationRequested)
            {
                var context = EndGetContext(result);

                Handler staticFilesHandler = new StaticFilesHandler();
                Handler endpointsHandler = new EndpointsHandler();
                staticFilesHandler.Successor = endpointsHandler;
                staticFilesHandler.HandleRequest(context); // it just called that function and went to call Receive() ? 

                if (!_token.IsCancellationRequested)
                    Receive();
            }
            else if (_token.IsCancellationRequested) Stop();
        }
        
        private IAsyncResult BeginGetContext(AsyncCallback? callback, object? state)
        {
            var getContext = ((HttpListener)state).GetContextAsync();
            IAsyncResult result = new TaskAsyncResult(getContext, state);
            if (callback != null)
            {
                getContext.ContinueWith(t => callback(result));
            }
        
            return result;
        }

        private HttpListenerContext EndGetContext(IAsyncResult result)
        {
            if (result is not TaskAsyncResult taskAsyncResult) throw new ArgumentException("Invalid IAsyncResult result provided");

            return ((Task<HttpListenerContext>)taskAsyncResult.InnerTask).Result; // so when EndGetContext is executed, the InnerTask is already done and this returns immediately?
        }
    }
}