using System;

namespace Client.Interfaces
{
    public class CustomProtocolHeaderFilter : FixedHeaderFilter<MyPackage>
    {
        public CustomProtocolHeaderFilter(int headerSize)
            : base(headerSize)
        {
        }

        public override int GetBodyLengthFromHeader(Span<byte> buffer)
        {
            Span<byte> boySizeBytes = new Span<byte>(new byte[2]);
            buffer.Slice(0, 2).CopyTo(boySizeBytes);
            if (BitConverter.IsLittleEndian)
            {
                boySizeBytes.Reverse();
            }
            ushort bodySize = BitConverter.ToUInt16(boySizeBytes);
            return bodySize;
        }

        public ushort GetIdFromHeader(Span<byte> buffer)
        {
            Span<byte> idBytes = new Span<byte>(new byte[2]);
            buffer.Slice(2, 2).CopyTo(idBytes);
            if (BitConverter.IsLittleEndian)
            {
                idBytes.Reverse();
            }
            ushort id = BitConverter.ToUInt16(idBytes);
            return id;
        }

        public override MyPackage DecodePackage(Span<byte> buffer)
        {
            int bodyLength = GetBodyLengthFromHeader(buffer);
            byte[] body = ByteArrayPool.Get(bodyLength);
            buffer.Slice(HeaderSize, bodyLength).CopyTo(body);

            MyPackage package = new MyPackage();
            package.BodyLength = (ushort)bodyLength;
            package.Id = GetIdFromHeader(buffer);
            package.Type = buffer[4];
            package.Body = body;
            return package;
        }
    }
}
