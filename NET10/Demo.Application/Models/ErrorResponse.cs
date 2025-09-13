namespace Demo.Application.Models;

/// <summary>
/// API 오류 응답 모델
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 오류 코드 (선택사항)
    /// </summary>
    public string? ErrorCode { get; set; }
    
    /// <summary>
    /// 상세 오류 정보 (선택사항)
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// 타임스탬프
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}