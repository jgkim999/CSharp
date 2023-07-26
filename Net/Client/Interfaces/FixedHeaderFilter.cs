using System;

namespace Client.Interfaces
{
    public abstract class FixedHeaderFilter<Package>
    {
        private readonly int _headerSize;
        public int HeaderSize { get { return _headerSize; } }

        public FixedHeaderFilter(int headerSize)
        {
            _headerSize = headerSize;
        }

        public abstract int GetBodyLengthFromHeader(Span<byte> buffer);
        public abstract Package DecodePackage(Span<byte> buffer);
    }
}
