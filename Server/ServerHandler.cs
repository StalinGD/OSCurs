using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Server
{
    public abstract class ServerHandler
    {

        public abstract void HandleMessage(MessageReader reader, MessageWriter writer, out int writed);


    }
}
