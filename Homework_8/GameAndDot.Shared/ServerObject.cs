using GameAndDot.Shared.Models;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace GameAndDot.Shared
{
    public class ServerObject
    {
        TcpListener tcpListener = new TcpListener(IPAddress.Any, 8888);
        public List<ClientObject> Players { get; private set; } = new();

        public List<EventMessage> History { get; private set; } = new();

        public void RemoveConnection(string id)
        {
            // получаем по id закрытое подключение
            ClientObject? player = Players.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (player != null) Players.Remove(player);
            player?.Close();
        }


        public async Task ListenAsync()
        {
            try
            {
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Players.Add(clientObject);

                    Task.Run(clientObject.ProcessAsync);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }

        // трансляция сообщения подключенным клиентам
        public async Task BroadcastMessageAsync(string message, string id)
        {
            foreach (var player in Players)
            {
                try
                {
                    if (player.Id != id) // если id клиента не равно id отправителя
                    {
                        await player.Writer.WriteLineAsync(message); //передача данных
                        await player.Writer.FlushAsync();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"error in translation:  {ex}");
                }
               
            }
        }

        public async Task BroadcastMessageAllAsync(string message)
        {
            foreach (var player in Players)
            {
                await player.Writer.WriteLineAsync(message); //передача данных
                await player.Writer.FlushAsync();
            }
        }


        // отключение всех клиентов
        protected internal void Disconnect()
        {
            foreach (var player in Players)
            {
                player.Close(); //отключение клиента
            }
            tcpListener.Stop(); //остановка сервера
        }
    }
}
