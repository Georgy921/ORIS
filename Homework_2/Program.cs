using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HTTPServer.Core;
using HTTPServer.Core.Handlers;
using HTTPServer.shared;

try
{
    
    string settings = File.ReadAllText("settings.json");
    SettingsModel settingsModel = JsonSerializer.Deserialize<SettingsModel>(settings);
    

    HttpListener server = new HttpListener();
    // установка адресов прослушки
    server.Prefixes.Add("http://" + settingsModel.Domain + ":" + settingsModel.Port + '/');
    server.Start(); // начинаем прослушивать входящие подключения
    using var cts = new CancellationTokenSource();
    
    Console.WriteLine("Server is started");
    var serverTask = ProcessRequest(server, settingsModel, cts.Token);
    do
    {
        string? input = await Console.In.ReadLineAsync();
        if (input?.Trim().ToLower() == "stop" || input?.Trim().ToLower() == ".stop")
        {
            Console.WriteLine("Остановка сервера...");
            cts.Cancel(); // Отменяем обработку
            server.Stop(); // Останавливаем прослушку
            break;
        }
        else
        {
            Console.WriteLine("Такой команды не существует");
        }
    } while (true);
    
}
catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException)
{
    Console.WriteLine("settings are not found");
}
catch (JsonException e)
{
    Console.WriteLine("settings.json is incorrect");
}
catch (Exception e) { Console.WriteLine("There is an exception: " + e.Message); }

static async Task ProcessRequest(HttpListener server, SettingsModel settingsModel, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        Console.WriteLine("Server is awaiting for request");

        // получаем контекст

        var context = await server.GetContextAsync();

        Handler staticFilesHandler = new StaticFilesHandler();
        Handler findMethodsHandler = new ConcreteHandler2();
        staticFilesHandler.Successor = findMethodsHandler;
        staticFilesHandler.HandleRequest(2);

        var request = context.Request;
        if (request.HttpMethod.Equals("get", StringComparison.OrdinalIgnoreCase))
        {
            var response = context.Response;
            // отправляемый в ответ код html возвращает

            try
            {
                var path = context.Request;
                var urlLocalPath = path.Url?.AbsolutePath;
                string responseText = File.ReadAllText(settingsModel.StaticDirectoryPath + urlLocalPath);
                response.Headers.Add("Content-Type", "text/html");
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);


                response.ContentLength64 = buffer.Length;
                using Stream output = response.OutputStream;
                await output.WriteAsync(buffer);
                await output.FlushAsync();

                Console.WriteLine("Запрос обработан");
            }

            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine("static folder not found");
                server.Stop();
                Console.WriteLine("Server is stopped");
                break;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("html is not found in folder");
                server.Stop();
                Console.WriteLine("Server is stopped");
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine("There is an exception: " + e.Message);
                server.Stop();
                Console.WriteLine("Server is stopped");
                break;
            }
        }
        else
        {
            if (request.HttpMethod.Equals("post", StringComparison.OrdinalIgnoreCase))
            {
                switch (request.Url.AbsolutePath)
                {
                    case "SendEmail":
                        break;
                    default:
                        break;
                }
            }
        }
    }
}