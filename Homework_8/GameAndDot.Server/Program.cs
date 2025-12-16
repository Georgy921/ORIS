using GameAndDot.Shared;

namespace GameAndDot.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Server start");

            ServerObject server = new ServerObject();
            await server.ListenAsync();
        }
    }
}
