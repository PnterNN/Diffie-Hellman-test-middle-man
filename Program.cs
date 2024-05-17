using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MITMProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxy = new ProxyServer(901, 900);
            proxy.Start();
            Console.ReadLine();
        }
    }

    public class ProxyServer
    {
        private int _listenPort;
        private int _serverPort;

        public ProxyServer(int listenPort, int serverPort)
        {
            _listenPort = listenPort;
            _serverPort = serverPort;
        }

        public void Start()
        {
            Task.Run(() => ListenForClientConnections());
        }

        private async Task ListenForClientConnections()
        {
            var listener = new TcpListener(IPAddress.Loopback, _listenPort);
            listener.Start();
            Console.WriteLine($"Proxy server listening on 127.0.0.1:{_listenPort}");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected");
                HandleClient(client);
            }
        }

        private async void HandleClient(TcpClient client)
        {
            using (var server = new TcpClient())
            {
                await server.ConnectAsync(IPAddress.Loopback, _serverPort);
                Console.WriteLine("Connected to the real server");

                var clientToServer = Task.Run(() => RelayTraffic(client.GetStream(), server.GetStream(), "Client -> Server"));
                var serverToClient = Task.Run(() => RelayTraffic(server.GetStream(), client.GetStream(), "Server -> Client"));

                await Task.WhenAll(clientToServer, serverToClient);
            }
        }
        private async Task RelayTraffic(Stream fromStream, Stream toStream, string direction)
        {
            var buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = await fromStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"{direction}: {data}");
                await toStream.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }
}