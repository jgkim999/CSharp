using System.Buffers;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 바이너리 패킷 정보
/// 헤더(4바이트) + 바디로 구성된 패킷
/// - 처음 2바이트: 메시지 타입
/// - 다음 2바이트: 바디 길이
/// - 나머지: 바디 데이터
/// </summary>
public class BinaryPackageInfo : IDisposable
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    private byte[]? _rentedArray;
    private bool _disposed;

    /// <summary>
    /// 메시지 타입 (2바이트)
    /// </summary>
    public ushort MessageType { get; set; }

    /// <summary>
    /// 바디 길이 (2바이트)
    /// </summary>
    public ushort BodyLength { get; set; }

    /// <summary>
    /// 바디 데이터 (실제 사용되는 길이는 BodyLength만큼)
    /// 주의: 이 배열은 ArrayPool에서 빌린 것이므로 BodyLength보다 클 수 있습니다.
    /// </summary>
    public byte[] Body { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 바디 데이터의 실제 유효한 부분만 반환
    /// </summary>
    public ReadOnlySpan<byte> BodySpan => Body.AsSpan(0, BodyLength);

    /// <summary>
    /// ArrayPool에서 배열을 빌려서 Body에 할당합니다
    /// </summary>
    public void RentBody(int minimumLength)
    {
        if (minimumLength <= 0)
        {
            Body = Array.Empty<byte>();
            return;
        }

        _rentedArray = _arrayPool.Rent(minimumLength);
        Body = _rentedArray;
    }

    /// <summary>
    /// 바디 데이터를 복사하지 않고 직접 설정 (빈 배열인 경우)
    /// </summary>
    public void SetEmptyBody()
    {
        Body = Array.Empty<byte>();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_rentedArray != null)
        {
            _arrayPool.Return(_rentedArray);
            _rentedArray = null;
        }

        Body = Array.Empty<byte>();
        _disposed = true;
    }
}