using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public ref struct MessageReader
    {
        public Span<byte> Bytes { get; }
        private int currentPosition = 0;


        public MessageReader(Span<byte> bytes)
        {
            Bytes = bytes;
        }

        public MessageHeader ReadHeader()
        {
            var header = MessageHeader.FromMessage(Bytes);
            currentPosition += MessageHeader.GetByteLength();
            return header;
        }

        public int ReadInt32()
        {
            var len = sizeof(int);
            var value = BitConverter.ToInt32(GetNextBytes(len));
            currentPosition += len;
            return value;
        }

        public long ReadInt64()
        {
            var len = sizeof(long);
            var value = BitConverter.ToInt64(GetNextBytes(len));
            currentPosition += len;
            return value;
        }

        public string ReadString()
        {
            // null-terminated utf-8
            var nullPosition = Bytes.Length - 1;
            for (int i = currentPosition; i < Bytes.Length; i++)
            {
                if (Bytes[i] == 0)
                {
                    nullPosition = i;
                    break;
                }
            }
            var len = nullPosition - currentPosition;
            var value = Encoding.UTF8.GetString(GetNextBytes(len));
            currentPosition += len + 1;
            return value;
        }

        private Span<byte> GetNextBytes()
        {
            return Bytes[currentPosition..];
        }

        private Span<byte> GetNextBytes(int length)
        {
            return Bytes[currentPosition..(currentPosition + length)];
        }
    }
}
