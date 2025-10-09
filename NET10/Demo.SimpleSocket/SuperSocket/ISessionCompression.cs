using System.Buffers;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 세션 압축 인터페이스
/// </summary>
public interface ISessionCompression
{
    /// <summary>
    /// 데이터 압축 (ArrayPool 사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <param name="data">압축할 데이터</param>
    /// <param name="arrayPool">사용할 ArrayPool</param>
    /// <returns>(압축된 버퍼, 실제 데이터 길이)</returns>
    (byte[] Buffer, int Length) Compress(ReadOnlySpan<byte> data, ArrayPool<byte> arrayPool);

    /// <summary>
    /// 데이터 압축 해제 (ArrayPool 사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <param name="compressedData">압축 해제할 데이터</param>
    /// <param name="arrayPool">사용할 ArrayPool</param>
    /// <returns>(압축 해제된 버퍼, 실제 데이터 길이)</returns>
    (byte[] Buffer, int Length) Decompress(ReadOnlySpan<byte> compressedData, ArrayPool<byte> arrayPool);
}