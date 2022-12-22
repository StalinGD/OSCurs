using System;
using System.Text;
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Server
{
    internal sealed class Program
    {
        private const int DefaultPort1 = 4040;
        private const int DefaultPort2 = 4041;

        private static IConfiguration config;
        private static ILoggerFactory loggerFactory;

        private static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("Server application");
            var server1Command = new Command("server1", "First type server");
            rootCommand.Add(server1Command);
            var server2Command = new Command("server2", "Second type server");
            rootCommand.Add(server2Command);

            var portOption = new Option<int>
            (
                aliases: new string[] { "--port", "-p" },
                description: "Server port to listen on"
            );
            rootCommand.AddGlobalOption(portOption);

            config = BuildConfig(args);
            loggerFactory = BuildLoggerFactory(config);
            var logger = loggerFactory.CreateLogger<Program>();

            server1Command.SetHandler(async (port) =>
            {
                logger.LogInformation("Starting server type 1...");
                var server = CreateServer1(port);
                await server.ListenAsync();
            }, portOption);

            server2Command.SetHandler(async (port) =>
            {
                logger.LogInformation("Starting server type 2...");
                var server = CreateServer2(port);
                await server.ListenAsync();
            }, portOption);

            await rootCommand.InvokeAsync(args);
        }

        private static IConfiguration BuildConfig(string[] args)
        {
            return new ConfigurationBuilder()
                .AddJsonFile("config.json", true)
                .AddEnvironmentVariables("OSC-")
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

        private static Server CreateServer1(int port)
        {
            port = ResolvePort(port, "Port1", DefaultPort1);

            return new Server(
                () => new Server1ClientHandler(loggerFactory.CreateLogger<Server1ClientHandler>()),
                port,
                config,
                loggerFactory.CreateLogger<Server>()
            );
        }

        private static Server CreateServer2(int port)
        {
            port = ResolvePort(port, "Port2", DefaultPort2);

            return new Server(
                () => new Server2ClientHandler(loggerFactory.CreateLogger<Server2ClientHandler>()),
                port,
                config,
                loggerFactory.CreateLogger<Server>()
            );
        }


        private static int ResolvePort(int commandLinePort, string defaultConfigEntry, int defaultPort)
        {
            if (commandLinePort > 0)
            {
                return commandLinePort;
            }

            return config.GetValue("Port", config.GetValue(defaultConfigEntry, defaultPort));
        }
    }
}