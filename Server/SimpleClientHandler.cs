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
            bool ignoreDuplicates;

            switch (request.Code)
            {
                case MessageCode.SingleRequest:
                    ignoreDuplicates = reader.ReadBool();
                    writed = CreateAndWriteResponse(writer, request.Code, ignoreDuplicates);
                    return;

                case MessageCode.WatchRequest:
                    var period = TimeSpan.FromTicks(reader.ReadInt64());
                    ignoreDuplicates = reader.ReadBool();
                    SetNotifyMode(period, () => CreateAndWriteResponse(GetWriter(), request.Code, ignoreDuplicates));
                    return;

                default:
                    Logger.LogError("Unsupported request code {Code} reseived", request.Code);
                    return;
            }
        }

        protected abstract int CreateAndWriteResponse(MessageWriter writer, byte code, bool dontSendIfSame);
    }
}
