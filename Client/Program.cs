using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shared;

namespace Client
{
    internal class Program
    {
        private static TimeSpan watchPeriod = TimeSpan.FromSeconds(1);


        private static async Task Main(string[] args)
        {
            var endpointString = "127.0.0.1:4040";
            if (!IPEndPoint.TryParse(endpointString, out var endpoint))
            {
                Error("Endpoint is not valid");
                return;
            }

            var watchClient = false;
            var watchServer = false;
            Func<Socket, ArraySegment<byte>, Task> handler = SendSingleRequest;

            if (watchClient)
            {
                handler = WatchClientwise;
            }
            else if (watchServer) 
            {
                handler = WatchServerwise;
            }

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(endpoint);
                Out("Connected");

                var buffer = new byte[1024];

                await handler(socket, buffer);
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



        private static int WriteRequest(byte code, Span<byte> bytes, TimeSpan watchPeriod = default)
        {
            var writer = new MessageWriter(bytes);

            writer.Write(new MessageHeader(code, DateTime.Now));
            writer.Write(watchPeriod.Ticks);

            return writer.CurrentPosition;
        }

        private static void ReadMessage(Span<byte> bytes)
        {
            var reader = new MessageReader(bytes);

            var header = reader.ReadHeader();

            var architecture = reader.ReadString();
            var processorsCount = reader.ReadInt32();

            Out($"[{header.Timestamp}] {architecture}, {processorsCount}");
        }


        private static async Task SendSingleRequest(Socket socket, ArraySegment<byte> buffer)
        {
            var length = WriteRequest(MessageCode.SingleRequest, buffer);

            _ = await socket.SendAsync(buffer[..length], SocketFlags.None);

            var received = await socket.ReceiveAsync(buffer, SocketFlags.None);

            ReadMessage(buffer.AsSpan()[..received]);
        }

        private static async Task WatchClientwise(Socket socket, ArraySegment<byte> buffer)
        {
            while (true)
            {
                var length = WriteRequest(MessageCode.SingleRequest, buffer);

                _ = await socket.SendAsync(buffer[..length], SocketFlags.None);

                var received = await socket.ReceiveAsync(buffer, SocketFlags.None);

                ReadMessage(buffer.AsSpan()[..received]);

                await Task.Delay(watchPeriod);
            }
        }


        private static async Task WatchServerwise(Socket socket, ArraySegment<byte> buffer)
        {
            var length = WriteRequest(MessageCode.WatchRequest, buffer, watchPeriod);
            _ = await socket.SendAsync(buffer[..length], SocketFlags.None);

            while (true)
            {
                var received = await socket.ReceiveAsync(buffer, SocketFlags.None);

                ReadMessage(buffer.AsSpan()[..received]);
            }
        }
    }
}