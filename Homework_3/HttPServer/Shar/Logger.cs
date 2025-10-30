using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttPServer.Shar
{
    public class Logger
    {
        public void PrintMessage(string message)
        {
            Console.WriteLine($"{message}");
        }

        public void ServerStarted()
        {
            Console.WriteLine("Сервер запустился");
        }

        public void ServerWaiting()
        {
            Console.WriteLine("Сервер ожидает");
        }

        public void ServerStop()
        {
            Console.WriteLine("Сервер остановлен");
        }
    }
}
