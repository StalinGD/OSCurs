using System.Net;
using System.Net.Sockets;
using System.Text;
using Shared;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var endpointString = "127.0.0.1:4040";
            if (!IPEndPoint.TryParse(endpointString, out var endpoint))
            {
                Error("Endpoint is not valid");
                return;
            }

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(endpoint);
                Out("Connected");

                var buffer = new byte[1024];
                while (true)
                {
                    var length = 1;
                    buffer[0] = 0x01; // REQUEST CODE
                    _ = await socket.SendAsync(buffer[..length], SocketFlags.None);

                    var received = await socket.ReceiveAsync(buffer, SocketFlags.None);

                    ReadMessage(buffer.AsSpan()[..received]);

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            catch (SocketException ex)
            {
                Error(ex.Message);
            }

            Console.ReadKey();
        }

        public static void Error(string message)
        {
            Console.WriteLine($"Error: {message}");
        }

        public static void Out(string message)
        {
            Console.WriteLine(message);
        }

        private static void ReadMessage(Span<byte> bytes)
        {
            var reader = new MessageReader(bytes);

            var header = reader.ReadHeader();

            var architecture = reader.ReadString();
            var processorsCount = reader.ReadInt32();

            Out($"[{header.Timestamp}] {architecture}, {processorsCount}");
        }
    }
}