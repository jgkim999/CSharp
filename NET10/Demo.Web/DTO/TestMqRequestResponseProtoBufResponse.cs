using Demo.Application.Models;

namespace Demo.Web.DTO;

/// <summary>
/// ProtoBuf 요청-응답 테스트 응답 DTO
/// </summary>
public class TestMqRequestResponseProtoBufResponse
{
    /// <summary>
    /// 테스트 성공 여부
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 전송한 요청 데이터
    /// </summary>
    public ProtobufRequest? RequestData { get; set; }

    /// <summary>
    /// 수신한 응답 데이터
    /// </summary>
    public ProtobufResponse? ResponseData { get; set; }

    /// <summary>
    /// 대상 큐 이름
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// 처리 시간 (Milliseconds)
    /// </summary>
    public double ProcessingTime { get; set; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; set; }
}