using System.Buffers;
using System.Buffers.Binary;
using SuperSocket.ProtoBase;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 고정 헤더 파이프라인 필터
/// 패킷 구조:
/// - 2바이트: 메시지 타입 (ushort, BigEndian)
/// - 2바이트: 바디 길이 (ushort, BigEndian)
/// - N바이트: 바디 데이터
/// </summary>
public class FixedHeaderPipelineFilter : FixedHeaderPipelineFilter<BinaryPackageInfo>
{
    /// <summary>
    /// 생성자
    /// 헤더 크기는 4바이트 (메시지 타입 2바이트 + 바디 길이 2바이트)
    /// </summary>
    public FixedHeaderPipelineFilter() : base(4)
    {
    }

    /// <summary>
    /// 헤더에서 바디 길이를 추출합니다
    /// </summary>
    /// <param name="buffer">헤더 버퍼 (4바이트)</param>
    /// <returns>바디 길이</returns>
    protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
    {
        // 단일 세그먼트인 경우 fast path 사용 (대부분의 경우)
        if (buffer.IsSingleSegment)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(buffer.FirstSpan.Slice(2, 2));
        }

        // 여러 세그먼트인 경우 (드문 경우)
        var reader = new SequenceReader<byte>(buffer);

        // 처음 2바이트(메시지 타입)는 건너뜀
        reader.Advance(2);

        // 다음 2바이트에서 바디 길이를 읽음 (BigEndian)
        Span<byte> lengthBytes = stackalloc byte[2];
        reader.TryCopyTo(lengthBytes);

        return BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);
    }

    /// <summary>
    /// 헤더와 바디를 합쳐서 패킷 객체로 디코딩합니다
    /// </summary>
    /// <param name="buffer">전체 패킷 버퍼 (헤더 + 바디)</param>
    /// <returns>디코딩된 패킷 정보</returns>
    protected override BinaryPackageInfo DecodePackage(ref ReadOnlySequence<byte> buffer)
    {
        ushort messageType;
        ushort bodyLength;

        var packageInfo = new BinaryPackageInfo();

        // 버퍼가 단일 세그먼트인 경우 (대부분의 경우)
        if (buffer.IsSingleSegment)
        {
            var span = buffer.FirstSpan;

            // 메시지 타입 읽기 (처음 2바이트) - stackalloc 없이 직접 읽기
            messageType = BinaryPrimitives.ReadUInt16BigEndian(span);

            // 바디 길이 읽기 (다음 2바이트)
            bodyLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2));

            // 바디 데이터 - ArrayPool에서 빌려서 복사
            if (bodyLength > 0)
            {
                packageInfo.RentBody(bodyLength);
                span.Slice(4, bodyLength).CopyTo(packageInfo.Body);
            }
            else
            {
                packageInfo.SetEmptyBody();
            }
        }
        else
        {
            // 버퍼가 여러 세그먼트인 경우 (드문 경우)
            var reader = new SequenceReader<byte>(buffer);

            // 메시지 타입 읽기
            Span<byte> messageTypeBytes = stackalloc byte[2];
            reader.TryCopyTo(messageTypeBytes);
            messageType = BinaryPrimitives.ReadUInt16BigEndian(messageTypeBytes);
            reader.Advance(2);

            // 바디 길이 읽기
            Span<byte> bodyLengthBytes = stackalloc byte[2];
            reader.TryCopyTo(bodyLengthBytes);
            bodyLength = BinaryPrimitives.ReadUInt16BigEndian(bodyLengthBytes);
            reader.Advance(2);

            // 바디 데이터 읽기 - ArrayPool에서 빌려서 복사
            if (bodyLength > 0)
            {
                packageInfo.RentBody(bodyLength);
                reader.TryCopyTo(packageInfo.Body);
            }
            else
            {
                packageInfo.SetEmptyBody();
            }
        }

        packageInfo.MessageType = messageType;
        packageInfo.BodyLength = bodyLength;

        return packageInfo;
    }
}