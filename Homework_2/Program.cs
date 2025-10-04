using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
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
        var response = context.Response;
        // отправляемый в ответ код html возвращает
        try
        {
            string responseText = File.ReadAllText(settingsModel.StaticDirectoryPath + "browser.html");
            response.Headers.Add("Content-Type", "text/html");
            byte[] buffer = Encoding.UTF8.GetBytes(responseText);
            // получаем поток ответа и пишем в него ответ
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            // отправляем данные
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
            Console.WriteLine("browser.html is not found in static folder");
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
}