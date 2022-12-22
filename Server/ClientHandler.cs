using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Shared;

namespace Server
{
    public abstract class ClientHandler
    {
        protected ILogger Logger { get; private set; }
        protected int Id { get; private set; }

        private Socket socket;
        private Task loop;
        private Func<Task> currentMode = null;

        private readonly byte[] buffer = new byte[1024];


        public ClientHandler(ILogger logger)
        {
            Logger = logger;
        }

        public void HandleClient(int id, Socket socket)
        {
            Id = id;
            this.socket = socket;
            SetListingMode();
            loop = Task.Run(HandleClientAsync);
        }


        protected abstract void HandleRequest(MessageReader reader, MessageWriter writer, out int writed);

        protected void SetListingMode()
        {
            SetMode(ListenAsync);
        }
        protected void SetNotifyMode(TimeSpan period, Func<int> writeFunc)
        {
            SetMode(() => NotifyAsync(period, writeFunc));
        }

        protected MessageReader GetReader() => new (buffer.AsSpan());
        protected MessageWriter GetWriter() => new (buffer.AsSpan());


        private async Task HandleClientAsync()
        {
            while (true)
            {
                await currentMode();
            }
        }

        private void SetMode(Func<Task> mode)
        {
            currentMode = mode;
        }



        private async Task ListenAsync()
        {
            var received = await socket.ReceiveAsync(buffer, SocketFlags.None);

            HandleRequest(GetReader(), GetWriter(), out var toWrite);

            await Flush(toWrite);
        }

        private async Task NotifyAsync(TimeSpan period, Func<int> writeFunc)
        {
            var toWrite = writeFunc();

            await Flush(toWrite);

            await Task.Delay(period);
        }

        private async Task Flush(int length)
        {
            if (length > 0)
            {
                await socket.SendAsync(buffer[..length], SocketFlags.None);
            }
        }
    }
}
