using System.Collections.Concurrent;
using System.Threading;

namespace Client
{
    public static class ByteArrayPool
    {
        private static ConcurrentDictionary<long, ConcurrentQueue<byte[]>> InternalPool = new ConcurrentDictionary<long, ConcurrentQueue<byte[]>>();
        private static long TotalAllocated = 0;
        private static long TotalReleased = 0;

        public static byte[] Get(long size)
        {
            Interlocked.Add(ref TotalAllocated, size);

            ConcurrentQueue<byte[]> queue;
            if (InternalPool.ContainsKey(size))
            {
                queue = InternalPool[size];
            }
            else
            {
                queue = new ConcurrentQueue<byte[]>();
                InternalPool[size] = queue;
            }

            if (queue.TryDequeue(out var buffer) == true)
            {
                return buffer;
            }
            else
            {
                return new byte[size];
            }
        }

        public static void Release(byte[] buffer)
        {
            if (buffer.Length == 0)
                return;

            Interlocked.Add(ref TotalReleased, buffer.Length);

            ConcurrentQueue<byte[]> queue;
            if (InternalPool.ContainsKey(buffer.Length))
            {
                InternalPool[buffer.Length].Enqueue(buffer);
            }
            else
            {
                queue = new ConcurrentQueue<byte[]>();
                InternalPool[buffer.Length] = queue;
                queue.Enqueue(buffer);
            }
        }

        public static long Size()
        {
            long totalSize = 0;
            foreach (var item in InternalPool)
            {
                totalSize += (item.Key * item.Value.Count);
            }
            return totalSize;
        }

        /// <summary>
        /// 통계
        /// </summary>
        /// <returns>
        /// TotalAllocated 총 할당
        /// TotalReleased 총 반납
        /// TotalPooled 현재 pool size
        /// NotReleased 반환되지 않은 크기 (현재 사용중인 크기)
        /// </returns>
        public static (long TotalAllocated, long TotalReleased, long TotalPooled, long NotReleased) Stat()
        {
            return (TotalAllocated, TotalReleased, Size(), TotalAllocated - TotalReleased);
        }

        public static void Clear()
        {
            InternalPool.Clear();
        }
    }
}
