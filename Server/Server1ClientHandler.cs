using Microsoft.Extensions.Logging;
using Shared;
using Server.OSApi;

namespace Server
{
    /// <summary>
    /// Responds with architecture and logical cores count.
    /// <br/>
    /// Not ideal name, but pretty straightforward,
    /// sinse this handler serves the first part of the task.
    /// </summary>
    internal class Server1ClientHandler : SimpleClientHandler
    {
        private bool isNotFirstRequest;
        private string lastArchitecture;
        private int lastLogicalCores;

        public Server1ClientHandler(ILogger logger) : base(logger) { }


        protected override int CreateAndWriteResponse(MessageWriter writer, byte code, bool dontSendIfSame)
        {
            var header = new MessageHeader(code, DateTime.Now);

            var architecture = OperatingSystemApi.Current.GetArchitecture();
            var logicalCores = OperatingSystemApi.Current.GetLogicalCoresCount();

            if (dontSendIfSame &&
                isNotFirstRequest &&
                lastArchitecture == architecture &&
                lastLogicalCores == logicalCores)
            {
                return 0; // Ignore and send nothing
            }

            lastArchitecture = architecture;
            lastLogicalCores = logicalCores;
            isNotFirstRequest = true;

            writer.Write(header);
            writer.Write(architecture);
            writer.Write(logicalCores);
            return writer.CurrentPosition;
        }
    }
}