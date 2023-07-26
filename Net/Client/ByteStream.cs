using Client.Exceptions;

using System;

namespace Client
{
    public class ByteStream
    {
        protected byte[] _srcByteArray;
        private int _maxSizePacket;

        public long Position
        {
            get; set;
        }

        public long Length => _srcByteArray.Length;

        public ByteStream(int capacity, int maxSizePacket)
        {
            _srcByteArray = ByteArrayPool.Get(capacity);
            Position = 0;
            _maxSizePacket = maxSizePacket;
        }

        public ByteStream(byte[] sourceBuffer, int maxSizePacket)
        {
            _srcByteArray = sourceBuffer;
            Position = 0;
            _maxSizePacket = maxSizePacket;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <exception cref="OverSizedMessageException">메시지 크기 위반</exception>
        private void CheckBuffer(long count)
        {
            if (count <= 0 || count > _maxSizePacket)
            {
                throw new OverSizedMessageException($"msg size over {count} > {_maxSizePacket}");
            }

            var currentLength = _srcByteArray.Length;
            var leftByte = currentLength - Position;
            if (leftByte < count)
            {
                var newByteArray = ByteArrayPool.Get((currentLength + count) * 2);
                Array.Copy(_srcByteArray, newByteArray, Position);
                var releaseArray = _srcByteArray;
                _srcByteArray = newByteArray;
                ByteArrayPool.Release(releaseArray);
            }
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="OverSizedMessageException">메시지 크기 위반</exception>
        public void Write(byte[] buffer, long offset, long count)
        {
            CheckBuffer(count);
            Array.Copy(buffer, offset, _srcByteArray, Position, count);
            Position += count;
        }
        /*
        public bool IsComplete()
        {
            if (Position < PacketDefine.HEAD_SIZE)
            {
                return false;
            }
            var dataSize = GetHeadDataSize();
            var completeSize = PacketDefine.HEAD_SIZE + dataSize;
            if (Position < completeSize)
            {
                return false;
            }
            return true;
        }
        */
        /*
        private ushort GetHeadMsgType()
        {
            Array.Copy(_srcByteArray, 0, _typeBytes, 0, PacketDefine.TYPE);
            var type = BinaryPrimitives.ReadUInt16BigEndian(_typeBytes.AsSpan());
            return type;
        }
        */
        /*
        private ushort GetHeadDataSize()
        {
            Array.Copy(_srcByteArray, PacketDefine.TYPE, _sizeBytes, 0, PacketDefine.SIZE);
            var size = BinaryPrimitives.ReadUInt16BigEndian(_sizeBytes.AsSpan());
            return size;
        }
        */
        /*
        public (MsgType type, byte[] buffer) GetMessage()
        {
            var type = GetHeadMsgType();
            var dataSize = GetHeadDataSize();
            var buffer = ByteArrayPool.Get(dataSize);
            Array.Copy(_srcByteArray, PacketDefine.HEAD_SIZE, buffer, 0, dataSize);
            var nextDataIndex = PacketDefine.HEAD_SIZE + dataSize;
            var leftLength = Position - nextDataIndex;
            if (leftLength > 0)
            {
                Array.Copy(_srcByteArray, nextDataIndex, _srcByteArray, 0, leftLength);
                Position = leftLength;
            }
            else
            {
                Position = 0;
            }
            return ((MsgType)type, buffer);
        }
        */
        public void Advance(int count)
        {
            Array.Copy(_srcByteArray, count, _srcByteArray, 0, Position - count);
            Position = Position - count;
        }

        public Span<byte> GetBytes(int offset, int headerSize)
        {
            Span<byte> header = new Span<byte>(_srcByteArray, offset, headerSize);
            return header;
        }

        public void Release()
        {
            //ByteArrayPool.Release(_typeBytes);
            //ByteArrayPool.Release(_sizeBytes);
            ByteArrayPool.Release(_srcByteArray);
        }
    }
}
