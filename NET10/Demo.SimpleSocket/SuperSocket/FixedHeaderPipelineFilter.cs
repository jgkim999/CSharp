using System.Buffers;
using System.Buffers.Binary;
using SuperSocket.ProtoBase;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 고정 헤더 파이프라인 필터
/// 패킷 구조:
/// - 1바이트: 플래그 (압축, 암호화 등)
/// - 2바이트: 시퀀스 번호 (ushort, BigEndian)
/// - 1바이트: 예약됨 (미래 사용)
/// - 2바이트: 메시지 타입 (ushort, BigEndian)
/// - 2바이트: 바디 길이 (ushort, BigEndian)
/// - N바이트: 바디 데이터
/// </summary>
public class FixedHeaderPipelineFilter : FixedHeaderPipelineFilter<BinaryPackageInfo>
{
    /// <summary>
    /// 생성자
    /// 헤더 크기는 8바이트 (플래그 1 + 시퀀스 2 + 예약 1 + 메시지타입 2 + 바디길이 2)
    /// </summary>
    public FixedHeaderPipelineFilter() : base(8)
    {
    }

    /// <summary>
    /// 헤더에서 바디 길이를 추출합니다
    /// </summary>
    /// <param name="buffer">헤더 버퍼 (8바이트)</param>
    /// <returns>바디 길이</returns>
    protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
    {
        // 단일 세그먼트인 경우 fast path 사용 (대부분의 경우)
        if (buffer.IsSingleSegment)
        {
            // 플래그(1) + 시퀀스(2) + 예약(1) + 메시지타입(2) = 6바이트 건너뛰고 바디 길이(2) 읽기
            return BinaryPrimitives.ReadUInt16BigEndian(buffer.FirstSpan.Slice(6, 2));
        }

        // 여러 세그먼트인 경우 (드문 경우)
        var reader = new SequenceReader<byte>(buffer);

        // 플래그(1) + 시퀀스(2) + 예약(1) + 메시지타입(2) = 6바이트 건너뜀
        reader.Advance(6);

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
        PacketFlags flags;
        ushort sequence;
        byte reserved;
        ushort messageType;
        ushort bodyLength;

        var packageInfo = new BinaryPackageInfo();

        // 버퍼가 단일 세그먼트인 경우 (대부분의 경우)
        if (buffer.IsSingleSegment)
        {
            var span = buffer.FirstSpan;

            // 플래그 읽기 (오프셋 0, 1바이트)
            flags = (PacketFlags)span[0];

            // 시퀀스 번호 읽기 (오프셋 1, 2바이트)
            sequence = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(1, 2));

            // 예약 필드 읽기 (오프셋 3, 1바이트)
            reserved = span[3];

            // 메시지 타입 읽기 (오프셋 4, 2바이트)
            messageType = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2));

            // 바디 길이 읽기 (오프셋 6, 2바이트)
            bodyLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6, 2));

            // 바디 데이터 - ArrayPool에서 빌려서 복사 (오프셋 8부터)
            if (bodyLength > 0)
            {
                packageInfo.RentBody(bodyLength);
                span.Slice(8, bodyLength).CopyTo(packageInfo.Body);
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

            // 플래그 읽기
            reader.TryRead(out byte flagsByte);
            flags = (PacketFlags)flagsByte;

            // 시퀀스 번호 읽기
            Span<byte> sequenceBytes = stackalloc byte[2];
            reader.TryCopyTo(sequenceBytes);
            sequence = BinaryPrimitives.ReadUInt16BigEndian(sequenceBytes);
            reader.Advance(2);

            // 예약 필드 읽기
            reader.TryRead(out reserved);

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

        packageInfo.Flags = flags;
        packageInfo.Sequence = sequence;
        packageInfo.Reserved = reserved;
        packageInfo.MessageType = messageType;
        packageInfo.BodyLength = bodyLength;

        return packageInfo;
    }
}