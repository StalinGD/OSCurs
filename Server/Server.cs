using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace Server
{
    public sealed class Server : IDisposable
    {
        private Socket listener;

        private readonly ServerHandler handler;
        private readonly IConfiguration config;
        private readonly ILogger logger;
        private readonly int defaultPort = 4040;


        public Server(ServerHandler handler, IConfiguration config, ILogger logger)
        {
            this.handler = handler;
            this.config = config;
            this.logger = logger;
        }


        public async Task ListenAsync()
        {
            var port = config.GetValue("Port", defaultPort);
            var endpoint = new IPEndPoint(IPAddress.Any, port);

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(endpoint);

                listener.Listen();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while starting server listener");
                return;
            }

            logger.LogInformation("Server listening on {Endpoint}", endpoint);

            while (true)
            {
                try
                {
                    var client = await listener.AcceptAsync();
                    await HandleClientAsync(client);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception on server listener");
                }
            }
        }

        private async Task HandleClientAsync(Socket client)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);

                handler.HandleMessage(new MessageReader(buffer.AsSpan()), new MessageWriter(buffer.AsSpan()), out var toWrite);

                await client.SendAsync(buffer[..toWrite], SocketFlags.None);
            }
        }


        public void Dispose()
        {
            listener.Dispose();
        }
    }
}
