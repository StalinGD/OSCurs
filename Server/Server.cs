using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private int nextClientId = 0;

        private readonly Func<ClientHandler> handlerFactory;
        private readonly IConfiguration config;
        private readonly ILogger logger;
        private readonly int port;


        public Server(Func<ClientHandler> handlerFactory, int port, IConfiguration config, ILogger logger)
        {
            if (handlerFactory == null || config == null || logger == null)
            {
                throw new ArgumentNullException("None of the arguments should be null");
            }

            this.handlerFactory = handlerFactory;
            this.config = config;
            this.logger = logger;
            this.port = port;
        }


        public async Task ListenAsync()
        {
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
                    var clientSocket = await listener.AcceptAsync();

                    var id = nextClientId++;

                    logger.LogInformation("Client {Id} connected", id);

                    var client = handlerFactory();
                    client.HandleClient(id, clientSocket);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception on server listener");
                }
            }
        }


        public void Dispose()
        {
            listener.Dispose();
        }
    }
}
