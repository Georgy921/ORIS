using System.Net;
using MiniHttpServer.Settings;
using MiniHttpServer.Server;
using System.Text.Json;

namespace MiniHttpServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; // what is this ??

            JsonEntity? settings = null;
            HttpServer? server = null;

            try
            {
                // Load configuration
                Console.WriteLine("📋 Loading configuration...");
                settings = Singleton.GetInstance().Settings;

                if (settings == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Error: Failed to load settings from settings.json");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Configuration loaded successfully");
                Console.ResetColor();
                Console.WriteLine($"   Domain: {settings.Domain}");
                Console.WriteLine($"   Port: {settings.Port}");
                Console.WriteLine();

                // Verify Public folder exists
                if (!Directory.Exists("Public"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  Warning: 'Public' folder not found. Creating it...");
                    Console.ResetColor();
                    Directory.CreateDirectory("Public");
                }

                // Verify index.html exists
                if (!File.Exists("Public/index.html"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  Warning: 'Public/index.html' not found");
                    Console.ResetColor();
                    Console.WriteLine("   Server will return 404 for root path requests");
                }
                
                CancellationTokenSource cts = new CancellationTokenSource();
                
                // Create and start server
                server = new HttpServer(settings);
                server.Start(cts.Token);

                // Wait a moment for server to start
                await Task.Delay(500);

                string url = $"http://{settings.Domain}:{settings.Port}/";
                string hotelsListing = $"http://{settings.Domain}:{settings.Port}/tours/list";
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("🚀 Server is running!");
                Console.ResetColor();
                Console.WriteLine($"   URL: {url}");
                Console.WriteLine($"   HotelsListing: {hotelsListing}");
                
                /*
                // Command loop
                var serverStop = Task.Run(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        string? command = Console.ReadLine()?.Trim().ToLower();
                        
                        switch (command)
                        {
                            case "/stop":
                                Console.WriteLine();
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Program /stop caught");
                                Console.ResetColor();
                                cts.Cancel(); 
                                break;

                            case "/clear":
                                Console.Clear();
                                break;

                            case "":
                                // Ignore empty input
                                break;

                            case null:
                                // Handle Ctrl+C
                                cts.Cancel(); 
                                break;

                            default:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"❌ Unknown command: {command}");
                                Console.ResetColor();
                                Console.WriteLine("   Use /stop, /status, or /clear");
                                Console.WriteLine();
                                break;
                        }
                    }
                }, cts.Token
                );
                */

                // Wait for server to stop gracefully
                // await Task.WhenAll(serverStart); // we await for /stop case(just to execute all lines from that case, not to wait for their finishing) to finish(where the command _listener.Stop is executed, but it doesn't waiting for it to finish and goes furhter(to that line));
                while (!cts.IsCancellationRequested)
                {
                    string? command = Console.ReadLine()?.Trim().ToLower();
                    if (command == "/stop")
                    {
                        Console.WriteLine("Program /stop caught");
                        cts.Cancel();
                        break;
                    }
                }
                
                //server.Stop(); 

                //await Task.WhenAll(serverStart);
                
                Console.WriteLine($"Server is listening: {server._listener.IsListening}");

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Server stopped successfully");
                Console.ResetColor();
            }
            catch (FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Error: settings.json file not found");
                Console.ResetColor();
                Console.WriteLine("   Please ensure 'Settings/settings.json' exists");
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Invalid JSON format in settings.json");
                Console.ResetColor();
                Console.WriteLine($"   Details: {ex.Message}");
            }
            catch (HttpListenerException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Failed to start HTTP listener");
                Console.ResetColor();
                Console.WriteLine($"   Details: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}