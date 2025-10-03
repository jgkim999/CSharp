using MemoryPack;

namespace Demo.Application.DTO;

/// <summary>
/// MemoryPack을 사용한 메시지 큐 발행 요청 DTO
/// RabbitMQ를 통해 MemoryPack 직렬화 방식으로 전송할 메시지의 내용을 담습니다
/// </summary>
[MemoryPackable]
public partial class MqPublishMemoryPackRequest
{
    /// <summary>
    /// 메시지 큐로 전송할 메시지 내용
    /// </summary>
    public string Message { get; set; } = "Hello MemoryPack MQ";

    /// <summary>
    /// 메시지 생성 시각
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 메시지 ID
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// MemoryPack을 사용한 두 번째 메시지 큐 발행 요청 DTO
/// 테스트 목적으로 사용됩니다
/// </summary>
[MemoryPackable]
public partial class MqPublishMemoryPackRequest2
{
    /// <summary>
    /// 사용자 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 사용자 이메일
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 생성 시각
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 활성화 여부
    /// </summary>
    public bool IsActive { get; set; }
}