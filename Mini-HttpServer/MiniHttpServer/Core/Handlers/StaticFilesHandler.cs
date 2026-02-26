using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Shared;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Handlers
{
    internal class StaticFilesHandler : Handler
    {
        private const string DEFAULT_FILE = "index.html";
        
        public override async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                bool isGetMethod = request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
                
                if (!isGetMethod)
                {
                    PassToSuccessor(context);
                    return;
                }

                string requestedPath = request.Url?.AbsolutePath ?? "/";
                
                string path = requestedPath.Trim('/');

                bool isRootPath = string.IsNullOrEmpty(path);
                bool isStaticFile = !string.IsNullOrEmpty(path) && path.Contains('.');

                if (isRootPath || isStaticFile)
                {
                    await HandleStaticFileRequest(context, path, isRootPath);
                }
                else
                {
                    PassToSuccessor(context);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ StaticFilesHandler error: {ex.Message}");
                Console.ResetColor();
                
                if (!response.OutputStream.CanWrite)
                    return;

                Send500Error(context, ex.Message);
            }
        }
        
        private async Task HandleStaticFileRequest(HttpListenerContext context, string path, bool isRootPath)
        {
            var response = context.Response;
            byte[]? buffer = null;
            string filePath = path;

            try
            {
                // Root path = serve index.html
                if (isRootPath)
                {
                    filePath = DEFAULT_FILE;
                }

                buffer = GetResponseBytes.Invoke(filePath);

                if (buffer != null)
                {
                    string contentType = ContentType.GetContentType(filePath);
                    
                    response.ContentType = contentType;
                    response.ContentLength64 = buffer.Length;
                    response.StatusCode = 200;

                    using (Stream output = response.OutputStream)
                    {
                        await output.WriteAsync(buffer, 0, buffer.Length);
                        await output.FlushAsync();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ 200 OK - {filePath} ({contentType}, {buffer.Length} bytes)");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                else
                {
                    Send404Error(context, filePath);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error serving file {filePath}: {ex.Message}");
                Console.ResetColor();
                
                Send500Error(context, ex.Message);
            }
        }
        
        private void Send404Error(HttpListenerContext context, string requestedFile)
        {
            try
            {
                string html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>404 - Not Found</title>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #333;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
        }}
        .error-container {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 500px;
        }}
        h1 {{
            color: #e74c3c;
            font-size: 72px;
            margin: 0;
        }}
        h2 {{
            color: #555;
            margin: 10px 0;
        }}
        p {{
            color: #777;
            line-height: 1.6;
        }}
        .file-name {{
            background: #f5f5f5;
            padding: 10px;
            border-radius: 5px;
            font-family: monospace;
            color: #e74c3c;
            margin: 20px 0;
        }}
        a {{
            display: inline-block;
            margin-top: 20px;
            padding: 10px 20px;
            background: #667eea;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            transition: background 0.3s;
        }}
        a:hover {{
            background: #5568d3;
        }}
    </style>
</head>
<body>
    <div class='error-container'>
        <h1>404</h1>
        <h2>File Not Found</h2>
        <p>The requested file could not be found on this server.</p>
        <div class='file-name'>{System.Web.HttpUtility.HtmlEncode(requestedFile)}</div>
        <a href='/'>← Back to Home</a>
    </div>
</body>
</html>";

                byte[] buffer = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html; charset=UTF-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = 404;

                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  404 Not Found - {requestedFile}");
                Console.ResetColor();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending 404 response: {ex.Message}");
            }
        }

        private void Send500Error(HttpListenerContext context, string errorMessage)
        {
            try
            {
                string html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>500 - Server Error</title>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #333;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
        }}
        .error-container {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 500px;
        }}
        h1 {{
            color: #c0392b;
            font-size: 72px;
            margin: 0;
        }}
        h2 {{
            color: #555;
            margin: 10px 0;
        }}
        p {{
            color: #777;
            line-height: 1.6;
        }}
        a {{
            display: inline-block;
            margin-top: 20px;
            padding: 10px 20px;
            background: #667eea;
            color: white;
            text-decoration: none;
            border-radius: 5px;
        }}
    </style>
</head>
<body>
    <div class='error-container'>
        <h1>500</h1>
        <h2>Internal Server Error</h2>
        <p>An error occurred while processing your request.</p>
        <a href='/'>← Back to Home</a>
    </div>
</body>
</html>";

                byte[] buffer = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html; charset=UTF-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = 500;

                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ 500 Internal Server Error - {errorMessage}");
                Console.ResetColor();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending 500 response: {ex.Message}");
            }
        }
        
        private void PassToSuccessor(HttpListenerContext context)
        {
            if (Successor != null)
            {
                Console.WriteLine($"🔄 Passing to next handler: {context.Request.Url?.AbsolutePath}");
                Successor.HandleRequest(context);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  No successor handler available for: {context.Request.Url?.AbsolutePath}");
                Console.ResetColor();
                Send404Error(context, context.Request.Url?.AbsolutePath ?? "unknown");
            }
        }
    }
}
