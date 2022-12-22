using Microsoft.Extensions.Logging;
using Shared;

namespace Server
{
    internal abstract class SimpleClientHandler : ClientHandler
    {
        public SimpleClientHandler(ILogger logger) : base(logger) { }

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
                    var periodTicks = reader.ReadInt64();
                    SetNotifyMode(TimeSpan.FromTicks(periodTicks), () => CreateAndWriteResponse(GetWriter(), request.Code));
                    return;

                default:
                    Logger.LogError("Unsupported request code {Code} reseived", request.Code);
                    return;
            }
        }

        protected abstract int CreateAndWriteResponse(MessageWriter writer, byte code);
    }
}
