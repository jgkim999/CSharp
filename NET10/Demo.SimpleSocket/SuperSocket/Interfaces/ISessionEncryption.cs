using System.Buffers;

namespace Demo.SimpleSocket.SuperSocket.Interfaces;

/// <summary>
/// 세션 암호화 인터페이스
/// </summary>
public interface ISessionEncryption : IDisposable
{
    /// <summary>
    /// 암호화 키
    /// </summary>
    byte[] Key { get; }

    /// <summary>
    /// 초기화 벡터
    /// </summary>
    byte[] IV { get; }

    /// <summary>
    /// 데이터 암호화 (ArrayPool 사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <param name="data">암호화할 데이터</param>
    /// <param name="arrayPool">사용할 ArrayPool</param>
    /// <returns>(암호화된 버퍼, 실제 데이터 길이)</returns>
    (byte[] Buffer, int Length) Encrypt(ReadOnlySpan<byte> data, ArrayPool<byte> arrayPool);

    /// <summary>
    /// 데이터 복호화 (ArrayPool 사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <param name="encryptedData">복호화할 데이터</param>
    /// <param name="arrayPool">사용할 ArrayPool</param>
    /// <returns>(복호화된 버퍼, 실제 데이터 길이)</returns>
    (byte[] Buffer, int Length) Decrypt(ReadOnlySpan<byte> encryptedData, ArrayPool<byte> arrayPool);
}