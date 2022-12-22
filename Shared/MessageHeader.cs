using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public struct MessageHeader
    {
        public byte Code { get; }
        public DateTime Timestamp { get; }

        public MessageHeader(byte code, DateTime timestamp)
        {
            Code = code;
            Timestamp = timestamp;
        }

        public int ToBytes(Span<byte> bytes)
        {
            bytes[0] = Code;
            _ = BitConverter.TryWriteBytes(bytes[1..9], Timestamp.ToBinary());
            return GetByteLength();
        }

        public static MessageHeader FromMessage(Span<byte> bytes)
        {
            var code = bytes[0];
            var timestamp = DateTime.FromBinary(BitConverter.ToInt64(bytes[1..9]));
            return new MessageHeader(code, timestamp);
        }

        public static int GetByteLength()
        {
            return 9;
        }
    }
}
