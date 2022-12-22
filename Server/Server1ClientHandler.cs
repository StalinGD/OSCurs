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
        public Server1ClientHandler(ILogger logger) : base(logger) { }


        protected override int CreateAndWriteResponse(MessageWriter writer, byte code)
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