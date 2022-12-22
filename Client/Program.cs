using System;
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

            var watchClient = false;
            var watchServer = false;
            var watchPeriod = TimeSpan.FromSeconds(1);


            var client = await ConnectAsync(endpoint);
            if (client == null) return;

            try
            {
                if (watchClient)
                {
                    await WatchClientwise(client, watchPeriod);
                }
                else if (watchServer)
                {
                    await WatchServerwise(client, watchPeriod);
                }
                else
                {
                    await SendSingleRequest(client);
                }
            }
            catch (SocketException ex)
            {
                Error(ex.Message);
                return;
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

        private static async Task<Client> ConnectAsync(IPEndPoint endpoint)
        {
            var client = new Client();
            try
            {
                await client.ConnectAsync(endpoint);
            }
            catch (SocketException ex)
            {
                Error(ex.Message);
                return null;
            }
            Out("Connected");
            return client;
        }



        private static int WriteRequest(byte code, Client client, TimeSpan watchPeriod = default)
        {
            var writer = client.GetWriter();

            writer.Write(new MessageHeader(code, DateTime.Now));
            writer.Write(watchPeriod.Ticks);

            return writer.CurrentPosition;
        }

        private static void ReadMessage(Client client)
        {
            var reader = client.GetReader();

            var header = reader.ReadHeader();

            var architecture = reader.ReadString();
            var processorsCount = reader.ReadInt32();

            Out($"[{header.Timestamp}] {architecture}, {processorsCount}");
        }


        private static async Task SendSingleRequest(Client client)
        {
            var length = WriteRequest(MessageCode.SingleRequest, client);
            await client.FlushAsync(length);
            await client.ReceiveAsync();
            ReadMessage(client);
        }

        private static async Task WatchClientwise(Client client, TimeSpan watchPeriod)
        {
            while (true)
            {
                await SendSingleRequest(client);

                await Task.Delay(watchPeriod);
            }
        }

        private static async Task WatchServerwise(Client client, TimeSpan watchPeriod)
        {
            var length = WriteRequest(MessageCode.WatchRequest, client, watchPeriod);
            await client.FlushAsync(length);

            while (true)
            {
                await client.ReceiveAsync();
                ReadMessage(client);
            }
        }
    }
}