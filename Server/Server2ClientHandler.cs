using Microsoft.Extensions.Logging;
using Shared;
using Server.OSApi;

namespace Server
{
    /// <summary>
    /// Responds with physical cores count and process modules count.
    /// <br/>
    /// Not ideal name, but pretty straightforward,
    /// sinse this handler serves the second part of the task.
    /// </summary>
    internal class Server2ClientHandler : SimpleClientHandler
    {
        private bool isNotFirstRequest;
        private int lastPhysicalCores;
        private int lastModulesCount;

        public Server2ClientHandler(ILogger logger) : base(logger) { }


        protected override int CreateAndWriteResponse(MessageWriter writer, byte code, bool dontSendIfSame)
        {
            var header = new MessageHeader(code, DateTime.Now);

            var physicalCores = OperatingSystemApi.Current.GetPhysicalCoresCount();
            var modulesCount = OperatingSystemApi.Current.GetProcessModulesCount();

            if (dontSendIfSame && 
                isNotFirstRequest &&
                lastPhysicalCores == physicalCores &&
                lastModulesCount == modulesCount)
            {
                return 0; // Ignore and send nothing
            }

            lastPhysicalCores = physicalCores;
            lastModulesCount = modulesCount;
            isNotFirstRequest = true;

            writer.Write(header);
            writer.Write(physicalCores);
            writer.Write(modulesCount);
            return writer.CurrentPosition;
        }
    }
}