using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HttPServer.Shar;
using MiniHttpServer.Sharer;

namespace MiniHttpServer.Sharer
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger log = new();
            try
            {

                var server = new HttpServer();
                server.Start();

                Thread consoleThread = new Thread(() =>
                {
                    if (Console.ReadLine() == "stop")
                    {
                        server.Stop();
                    }
                });

                consoleThread.Start();
                consoleThread.Join();
            }
            catch (Exception ex)
            {
                log.PrintMessage($"{ex.Message}");
            }
            finally
            {
                log.ServerStop();
            }

        }
    }
}