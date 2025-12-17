using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XProtocol;

namespace TCPClient
{
    public class XClient
    {
        public Action<XPacket> OnPacketReceived { get; set; }

        private readonly Queue<byte[]> _packetSendingQueue = new Queue<byte[]>();

        private Socket _socket;
        private IPEndPoint _serverEndPoint;

        public void Connect(string ip, int port)
        {
            Connect(new IPEndPoint(IPAddress.Parse(ip), port));
        }

        public void Connect(IPEndPoint server)
        {
            _serverEndPoint = server;

            _socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _socket.Connect(_serverEndPoint);

            Task.Run((Action)RecievePackets);
            Task.Run((Action)SendPackets);
        }

        public void QueuePacketSend(byte[] packet)
        {
            if (packet.Length > 256)
            {
                throw new Exception("Max packet size is 256 bytes.");
            }

            _packetSendingQueue.Enqueue(packet);
        }

        private void RecievePackets()
        {
            var buffer = new byte[256];
            var data = new List<byte>();

            while (true)
            {
                try
                {
                    int received = _socket.Receive(buffer);
                    if (received == 0) continue;

                    data.AddRange(buffer.Take(received));

                    int endMarkerIndex = -1;
                    for (int i = 0; i < data.Count - 1; i++)
                    {
                        if (data[i] == 0xFF && data[i + 1] == 0x00)
                        {
                            endMarkerIndex = i + 1;
                            break;
                        }
                    }

                    if (endMarkerIndex != -1)
                    {
                        var packetBytes = data.Take(endMarkerIndex + 1).ToArray();

                        data.RemoveRange(0, endMarkerIndex + 1);

                        var parsed = XPacket.Parse(packetBytes);
                        if (parsed != null)
                        {
                            OnPacketReceived?.Invoke(parsed);
                        }
                    }
                }
                catch (SocketException)
                {
                    Console.WriteLine("Connection lost.");
                    return;
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void SendPackets()
        {
            while (true)
            {
                if (_packetSendingQueue.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var packet = _packetSendingQueue.Dequeue();
                _socket.Send(packet);

                Thread.Sleep(100);
            }
        }
    }
}
