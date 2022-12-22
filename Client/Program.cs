using System;
using System.Net;
using System.Net.Sockets;
using System.CommandLine;
using Shared;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var server1Command = new Command("server1", "Connect to first type server");
            rootCommand.Add(server1Command);
            var server2Command = new Command("server2", "Connect to second type server");
            rootCommand.Add(server2Command);

            var watchClientOption = new Option<bool>
                ("--watch-client", "Send requests to server to obtain new state");
            var watchServerOption = new Option<bool>
                ("--watch-server", "Force server to send updates continiusly");
            var watchPeriodOption = new Option<double>(
                "--watch-period", 
                description: "Period in seconds of sending updates via --watch-client or --watch-server", 
                getDefaultValue: () => 1);
            watchPeriodOption.ArgumentHelpName = "seconds";

            rootCommand.AddOption(watchClientOption);
            rootCommand.AddOption(watchServerOption);
            rootCommand.AddOption(watchPeriodOption);

            var addressArgument = new Argument<IPAddress>(
                "address",
                description: "IPv4 server address",
                getDefaultValue: () => IPAddress.Loopback);


            var server1PortArgument = new Argument<int>(
                "port",
                description: "Server port",
                getDefaultValue: () => 4040);
            server1Command.AddArgument(addressArgument);
            server1Command.AddArgument(server1PortArgument);

            var server2PortArgument = new Argument<int>(
                "port",
                description: "Server port",
                getDefaultValue: () => 4041);
            server2Command.AddArgument(addressArgument);
            server2Command.AddArgument(server2PortArgument);



            server1Command.SetHandler(async (address, port, watchClient, watchServer, watchPeriodSeconds) =>
            {
                var watchPeriod = TimeSpan.FromSeconds(watchPeriodSeconds);
                await Run(address, port, watchClient, watchServer, watchPeriod, ReadServer1Message);
            }, addressArgument, server1PortArgument, watchClientOption, watchServerOption, watchPeriodOption);

            server2Command.SetHandler(async (address, port, watchClient, watchServer, watchPeriodSeconds) =>
            {
                var watchPeriod = TimeSpan.FromSeconds(watchPeriodSeconds);
                await Run(address, port, watchClient, watchServer, watchPeriod, ReadServer2Message);
            }, addressArgument, server2PortArgument, watchClientOption, watchServerOption, watchPeriodOption);


            await rootCommand.InvokeAsync(args);

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

        private static async Task Run
            (IPAddress address, int port, bool watchClient, bool watchServer, TimeSpan watchPeriod, Action<Client> messageHandler)
        {
            var endpoint = new IPEndPoint(address, port);

            var client = await ConnectAsync(endpoint);
            if (client == null) return;

            try
            {
                if (watchClient)
                {
                    await WatchClientwise(client, watchPeriod, messageHandler);
                }
                else if (watchServer)
                {
                    await WatchServerwise(client, watchPeriod, messageHandler);
                }
                else
                {
                    await SendSingleRequest(client, messageHandler);
                }
            }
            catch (SocketException ex)
            {
                Error(ex.Message);
                return;
            }

        }



        private static int WriteRequest(byte code, Client client, TimeSpan watchPeriod = default)
        {
            var writer = client.GetWriter();

            writer.Write(new MessageHeader(code, DateTime.Now));
            writer.Write(watchPeriod.Ticks);

            return writer.CurrentPosition;
        }

        private static void ReadServer1Message(Client client)
        {
            var reader = client.GetReader();

            var header = reader.ReadHeader();

            var architecture = reader.ReadString();
            var processorsCount = reader.ReadInt32();

            Out($"[{header.Timestamp}][Server1]\nArchitecture: \t{architecture} \nLogical Cores: \t{processorsCount}");
        }

        private static void ReadServer2Message(Client client)
        {
            var reader = client.GetReader();

            var header = reader.ReadHeader();

            var physicalCores = reader.ReadInt32();
            var modulesCount = reader.ReadInt32();

            Out($"[{header.Timestamp}][Server2]\nPhysical Cores: \t{physicalCores} \nModules Count: \t{modulesCount}");
        }


        private static async Task SendSingleRequest(Client client, Action<Client> messageHandler)
        {
            var length = WriteRequest(MessageCode.SingleRequest, client);
            await client.FlushAsync(length);
            await client.ReceiveAsync();
            messageHandler(client);
        }

        private static async Task WatchClientwise(Client client, TimeSpan watchPeriod, Action<Client> messageHandler)
        {
            while (true)
            {
                await SendSingleRequest(client, messageHandler);

                await Task.Delay(watchPeriod);
            }
        }

        private static async Task WatchServerwise(Client client, TimeSpan watchPeriod, Action<Client> messageHandler)
        {
            var length = WriteRequest(MessageCode.WatchRequest, client, watchPeriod);
            await client.FlushAsync(length);

            while (true)
            {
                await client.ReceiveAsync();
                messageHandler(client);
            }
        }
    }
}