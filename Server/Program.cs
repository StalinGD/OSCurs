using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;
using Server.OSApi;

namespace Server
{
    internal sealed class Program
    {
        private static async Task Main(string[] args)
        {
            var config = BuildConfig();

            var loggerFactory = BuildLoggerFactory(config);

            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation($"Starting server...");

            var server = new Server(
                () => new ServerHandler1(loggerFactory.CreateLogger<ClientHandler>()), 
                config, 
                loggerFactory.CreateLogger<Server>());
            await server.ListenAsync();
        }

        private static IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("config.json", true)
                .AddEnvironmentVariables("OSC")
                .Build();
        }

        private static ILoggerFactory BuildLoggerFactory(IConfiguration config)
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(config)
                    .AddConsole()
                    .AddDebug();
            });
        }
    }

    class ServerHandler1 : ClientHandler
    {
        public ServerHandler1(ILogger logger) : base(logger)
        {

        }

        protected override void HandleRequest(MessageReader reader, MessageWriter writer, out int writed)
        {
            var request = reader.ReadHeader();
            writed = 0;

            switch (request.Code)
            {
                case MessageCode.SingleRequest:
                    writed = CreateAndWriteResponse(writer, request.Code);
                    return;
                case MessageCode.WatchRequest:
                    SetNotifyMode(TimeSpan.FromSeconds(1), () => CreateAndWriteResponse(GetWriter(), request.Code));
                    return;
                default:
                    Logger.LogError("Unsupported request code {Code} reseived", request.Code);
                    return;
            }
        }

        private int CreateAndWriteResponse(MessageWriter writer, byte code)
        {
            var header = new MessageHeader(code, DateTime.Now);

            var architecture = OperatingSystemApi.Current.GetArchitecture();
            var logicalProcessors = OperatingSystemApi.Current.GetLogicalCoresCount();

            writer.Write(header);
            writer.Write(architecture);
            writer.Write(logicalProcessors);
            return writer.CurrentPosition;
        }
    }
}