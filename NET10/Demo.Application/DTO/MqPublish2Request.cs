using System.ComponentModel;
using MessagePack;

namespace Demo.Application.DTO;

/// <summary>
/// 메시지 큐 발행 요청을 위한 DTO 클래스
/// RabbitMQ를 통해 전송할 메시지의 내용을 담습니다
/// </summary>
[MessagePackObject]
public class MqPublishRequest2
{
    /// <summary>
    /// 메시지 큐로 전송할 메시지 내용
    /// 기본값으로 "Hello MQ"가 설정됩니다
    /// </summary>
    [Key(0)]
    [DefaultValue("Hello MQ")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
