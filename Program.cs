using SProjectServer.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
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
        private bool bruteForce = false;
        private byte[] masterKey;

        public ProxyServer(int listenPort, int serverPort)
        {
            _listenPort = listenPort;
            _serverPort = serverPort;
        }

        public void Start()
        {
            Task.Run(() => ListenForClientConnections());
            Console.WriteLine("Enter 'bruteforce' to toggle brute-force attack");
            while (true)
            {
                string input = Console.ReadLine();
                if (input.Equals("bruteforce", StringComparison.OrdinalIgnoreCase))
                {
                    bruteForce = !bruteForce;
                    Console.WriteLine(bruteForce ? "Bruteforce enabled" : "Bruteforce disabled");
                }
            }
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

        private static bool SearchWordInFile(string filePath, string searchWord)
        {
            try
            {
                foreach (string line in File.ReadLines(filePath))
                {
                    if (line.Trim().Equals(searchWord, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File read error: {ex.Message}");
            }
            return false;
        }

        private async Task RelayTraffic(Stream fromStream, Stream toStream, string direction)
        {
            var buffer = new byte[4096];
            int bytesRead;
            string[] words = { "flag", "Everyone", "selam" };
            while ((bytesRead = await fromStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                int offset = 0;

                byte opcode = buffer[offset];
                offset += 1;

                Console.WriteLine($"{direction}: Opcode: {opcode}");

                while (offset < bytesRead)
                {
                    if (offset + 4 > bytesRead)
                        break;

                    int messageLength = BitConverter.ToInt32(buffer, offset);
                    offset += 4;

                    if (offset + messageLength > bytesRead)
                        break;

                    string message = Encoding.UTF8.GetString(buffer, offset, messageLength);
                    byte[] messageBytes = new byte[messageLength];
                    Buffer.BlockCopy(buffer, offset, messageBytes, 0, messageLength);
                    offset += messageLength;

                    if (bruteForce)
                    {
                        if(masterKey == null)
                        {
                            _ = Task.Run(() =>
                            {
                                Console.WriteLine($"{direction}: Brute-force attack started");
                                Stopwatch stopwatch = Stopwatch.StartNew();
                                for (BigInteger i = 0; i < 100000; i++)
                                {
                                    try
                                    {
                                        byte[] _masterKey;
                                        using (SHA256 sha256 = SHA256.Create())
                                        {
                                            _masterKey = sha256.ComputeHash(i.ToByteArray());
                                        }
                                        string decryptedMessage = AesUtil.DecryptStringFromBytes_Aes(messageBytes, _masterKey);
                                        
                                        if (Array.Exists(words, element => element == decryptedMessage))
                                        {
                                            stopwatch.Stop();
                                            masterKey = _masterKey;
                                            Console.WriteLine($"{direction}: Decrypted Message: {decryptedMessage}");
                                            Console.WriteLine($"sharedKey: {i}");
                                            Console.WriteLine("Şifre Bulunma süresi: " + stopwatch.Elapsed.TotalMilliseconds + " milliseconds");
                                            break;
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                            });
                        }
                        else
                        {
                            string decryptedMessage = AesUtil.DecryptStringFromBytes_Aes(messageBytes, masterKey);
                            Console.WriteLine($"{direction}: Message: {decryptedMessage}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{direction}: Message: {message}");
                    }
                    
                }
                await toStream.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }
}
