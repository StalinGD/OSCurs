using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public ref struct MessageWriter
    {
        public Span<byte> Bytes { get; }
        public int CurrentPosition { get; private set; } = 0;


        public MessageWriter(Span<byte> bytes)
        {
            Bytes = bytes;
        }

        public int Write(MessageHeader header)
        {
            header.ToBytes(GetNextBytes());
            var len = MessageHeader.GetByteLength();
            CurrentPosition += len;
            return len;
        }

        public int Write(int value)
        {
            _ = BitConverter.TryWriteBytes(GetNextBytes(), value);
            var len = sizeof(int);
            CurrentPosition += len;
            return len;
        }

        public int Write(long value)
        {
            _ = BitConverter.TryWriteBytes(GetNextBytes(), value);
            var len = sizeof(long);
            CurrentPosition += len;
            return len;
        }

        public int Write(string value)
        {
            // null-terminated utf-8
            var len = Encoding.UTF8.GetBytes(value, GetNextBytes());
            CurrentPosition += len;
            GetNextBytes()[0] = 0; // null-termination
            CurrentPosition += 1;
            return len + 1;
        }

        private Span<byte> GetNextBytes()
        {
            return Bytes[CurrentPosition..];
        }
    }
}
