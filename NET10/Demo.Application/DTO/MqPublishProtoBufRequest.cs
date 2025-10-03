using ProtoBuf;

namespace Demo.Application.DTO;

/// <summary>
/// ProtoBuf를 사용한 메시지 큐 발행 요청 DTO
/// RabbitMQ를 통해 ProtoBuf 직렬화 방식으로 전송할 메시지의 내용을 담습니다
/// </summary>
[ProtoContract]
public class MqPublishProtoBufRequest
{
    /// <summary>
    /// 메시지 큐로 전송할 메시지 내용
    /// </summary>
    [ProtoMember(1)]
    public string Message { get; set; } = "Hello ProtoBuf MQ";

    /// <summary>
    /// 메시지 생성 시각
    /// </summary>
    [ProtoMember(2)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 메시지 ID
    /// </summary>
    [ProtoMember(3)]
    public int Id { get; set; }
}

/// <summary>
/// ProtoBuf를 사용한 두 번째 메시지 큐 발행 요청 DTO
/// 테스트 목적으로 사용됩니다
/// </summary>
[ProtoContract]
public class MqPublishProtoBufRequest2
{
    /// <summary>
    /// 사용자 이름
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 사용자 이메일
    /// </summary>
    [ProtoMember(2)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 생성 시각
    /// </summary>
    [ProtoMember(3)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 활성화 여부
    /// </summary>
    [ProtoMember(4)]
    public bool IsActive { get; set; }
}