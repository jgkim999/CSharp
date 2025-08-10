namespace Demo.Web.Configs;

/// <summary>
/// Rate Limiting 구성 설정 클래스
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// 구성 섹션 이름
    /// </summary>
    public const string SectionName = "RateLimit";

    /// <summary>
    /// 사용자 생성 엔드포인트 Rate Limiting 설정
    /// </summary>
    public UserCreateEndpointConfig UserCreateEndpoint { get; set; } = new();

    /// <summary>
    /// 전역 Rate Limiting 설정
    /// </summary>
    public GlobalRateLimitConfig Global { get; set; } = new();
}

/// <summary>
/// 사용자 생성 엔드포인트 Rate Limiting 구성 설정
/// </summary>
public class UserCreateEndpointConfig
{
    /// <summary>
    /// Rate Limiting 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 요청 제한 횟수 (기본값: 10회)
    /// </summary>
    public int HitLimit { get; set; } = 10;

    /// <summary>
    /// 윈도우 기간 (초, 기본값: 60초)
    /// </summary>
    public int DurationSeconds { get; set; } = 60;

    /// <summary>
    /// 클라이언트 식별을 위한 헤더 이름 (null이면 기본값 사용: X-Forwarded-For 또는 RemoteIpAddress)
    /// </summary>
    public string? HeaderName { get; set; }

    /// <summary>
    /// Rate Limit 초과시 응답 메시지
    /// </summary>
    public string ErrorMessage { get; set; } = "Too many requests. Please try again later.";

    /// <summary>
    /// Retry-After 헤더 값 (초)
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;
}

/// <summary>
/// 전역 Rate Limiting 구성 설정
/// </summary>
public class GlobalRateLimitConfig
{
    /// <summary>
    /// 로깅 활성화 여부
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Rate Limit 적용시 정보 로그 기록 여부
    /// </summary>
    public bool LogRateLimitApplied { get; set; } = true;

    /// <summary>
    /// Rate Limit 초과시 경고 로그 기록 여부
    /// </summary>
    public bool LogRateLimitExceeded { get; set; } = true;

    /// <summary>
    /// 클라이언트 IP 정보 로그 포함 여부
    /// </summary>
    public bool IncludeClientIpInLogs { get; set; } = true;

    /// <summary>
    /// 요청 횟수 정보 로그 포함 여부
    /// </summary>
    public bool IncludeRequestCountInLogs { get; set; } = true;
}