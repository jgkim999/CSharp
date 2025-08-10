namespace Demo.Application.DTO;

/// <summary>
/// Rate Limit 초과시 반환되는 응답 모델
/// </summary>
public class RateLimitResponse
{
    /// <summary>
    /// HTTP 상태 코드 (429 Too Many Requests)
    /// </summary>
    public int StatusCode { get; set; } = 429;

    /// <summary>
    /// 사용자 친화적인 에러 메시지
    /// </summary>
    public string Message { get; set; } = "Too many requests. Please try again later.";

    /// <summary>
    /// 에러 코드
    /// </summary>
    public string ErrorCode { get; set; } = "RATE_LIMIT_EXCEEDED";

    /// <summary>
    /// 재시도 가능한 시간 (초)
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;

    /// <summary>
    /// 추가 세부 정보
    /// </summary>
    public string? Details { get; set; }
}