using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Shared;

namespace Client
{
    public class Client
    {
        private Socket socket;
        private readonly byte[] buffer = new byte[1024];


        public Client()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task ConnectAsync(IPEndPoint endpoint)
        {
            await socket.ConnectAsync(endpoint);
        }

        public MessageReader GetReader() => new(buffer.AsSpan());
        public MessageWriter GetWriter() => new(buffer.AsSpan());

        public async Task FlushAsync(int length)
        {
            if (length > 0)
            {
                await socket.SendAsync(buffer[..length], SocketFlags.None);
            }
        }

        public async Task<int> ReceiveAsync()
        {
            return await socket.ReceiveAsync(buffer, SocketFlags.None);
        }
    }
}
