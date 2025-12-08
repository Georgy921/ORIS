using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HttpServer;
using HttPServer.Settings;
using HttPServer.Suka;
using HttPServer.Utils;
using MiniHttpServer.Sharer;
using MiniHttpServer.Utils;

namespace MiniHttpServer.Sharer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                Global.Server = new HTTPServer();
                Global.Logger = Logger.Instance;
                Global.Settings = Singletone.Instance;


                Global.Server.Start();

                Console.WriteLine("нажмите /stop для завершения.\n");

                while (true)
                {

                    var stop = Console.ReadLine();
                    if (stop?.Trim().ToLower() == "/stop")
                    {
                        Global.Server.Stop();
                        break;
                    }
                    else Global.Logger.LogMess("wrong command");
                }

                Global.Server.Stop();
            }
            catch (Exception ex)
            {
                Global.Logger.LogMess(ex.Message);
            }
            finally
            {
                Global.Logger.ServerStopped();
            }

        }
    }
}