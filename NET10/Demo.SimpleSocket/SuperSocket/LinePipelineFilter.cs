using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 라인 기반 파이프라인 필터
/// 개행 문자(\r\n)로 구분되는 텍스트 메시지를 처리합니다
/// </summary>
public class LinePipelineFilter : TerminatorPipelineFilter<TextPackageInfo>
{
    /// <summary>
    /// 생성자
    /// \r\n (CRLF)를 메시지 종료자로 사용합니다
    /// </summary>
    public LinePipelineFilter() : base(new[] { (byte)'\r', (byte)'\n' })
    {
    }

    /// <summary>
    /// 버퍼에서 패킷을 디코딩합니다
    /// </summary>
    /// <param name="buffer">수신된 데이터 버퍼</param>
    /// <returns>디코딩된 패킷 정보</returns>
    protected override TextPackageInfo DecodePackage(ref ReadOnlySequence<byte> buffer)
    {
        return new TextPackageInfo
        {
            Text = buffer.GetString(Encoding.UTF8)
        };
    }
}