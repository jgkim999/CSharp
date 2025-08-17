using System.Collections.Concurrent;
using System.Text.Json;
using Demo.Application.Configs;
using Demo.Application.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Middleware;

/// <summary>
/// Rate Limit 응답을 사용자 정의 형식으로 변환하고 로깅을 처리하는 미들웨어
/// </summary>
public class RateLimitMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<RateLimitMiddleware> _logger;
  private readonly RateLimitConfig _rateLimitConfig;

  /// <summary>
  /// 클라이언트별 요청 카운터 (메모리 기반)
  /// </summary>
  private static readonly ConcurrentDictionary<string, ClientRequestInfo> _requestCounters = new();

  /// <summary>
  /// 클라이언트 요청 정보를 저장하는 클래스
  /// </summary>
  private class ClientRequestInfo
  {
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime LastRequestTime { get; set; }
  }

  /// <summary>
  /// JSON 직렬화 옵션 (성능 최적화를 위해 캐시됨)
  /// </summary>
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
  };

  /// <summary>
  /// RateLimitMiddleware의 새 인스턴스를 초기화합니다.
  /// </summary>
  /// <param name="next">다음 미들웨어 델리게이트</param>
  /// <param name="logger">로거 인스턴스</param>
  /// <param name="rateLimitConfig">Rate Limit 설정</param>
  public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, RateLimitConfig rateLimitConfig)
  {
    _next = next;
    _logger = logger;
    _rateLimitConfig = rateLimitConfig;
  }

  /// <summary>
  /// 미들웨어를 실행하고 Rate Limit 관련 로깅을 처리합니다.
  /// </summary>
  /// <param name="context">HTTP 컨텍스트</param>
  /// <returns>비동기 작업</returns>
  public async Task InvokeAsync(HttpContext context)
  {
    // Rate Limiting이 적용되는 엔드포인트인지 확인
    var isRateLimitedEndpoint = IsRateLimitedEndpoint(context.Request.Path);
    
    if (isRateLimitedEndpoint && _rateLimitConfig.Global.EnableLogging)
    {
      // 요청 전 로깅 처리
      await LogRequestInfo(context);
    }

    // 응답을 가로채기 위해 원본 응답 스트림을 저장
    var originalBodyStream = context.Response.Body;

    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;

    try
    {
      // 다음 미들웨어 실행
      await _next(context);

      // Rate Limit 응답(429)인지 확인
      if (context.Response.StatusCode == 429)
      {
        await HandleRateLimitResponse(context);
      }
      else
      {
        // 일반 응답인 경우 원본 스트림으로 복사
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        await context.Response.Body.CopyToAsync(originalBodyStream);
      }
    }
    finally
    {
      // 원본 응답 스트림 복원
      context.Response.Body = originalBodyStream;
    }
  }

  /// <summary>
  /// Rate Limiting이 적용되는 엔드포인트인지 확인합니다.
  /// </summary>
  /// <param name="path">요청 경로</param>
  /// <returns>Rate Limiting 적용 여부</returns>
  private bool IsRateLimitedEndpoint(string path)
  {
    // 현재는 사용자 생성 엔드포인트만 Rate Limiting 적용
    return path.Equals("/api/user/create", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// 요청 정보를 로깅하고 요청 카운터를 업데이트합니다.
  /// </summary>
  /// <param name="context">HTTP 컨텍스트</param>
  /// <returns>비동기 작업</returns>
  private async Task LogRequestInfo(HttpContext context)
  {
    var clientIp = GetClientIpAddress(context);
    var endpoint = context.Request.Path.ToString();
    var now = DateTime.UtcNow;

    // 클라이언트별 요청 카운터 업데이트
    var requestInfo = _requestCounters.AddOrUpdate(clientIp, 
      new ClientRequestInfo 
      { 
        RequestCount = 1, 
        WindowStart = now, 
        LastRequestTime = now 
      },
      (key, existing) =>
      {
        // 윈도우가 만료되었는지 확인 (60초)
        if (now.Subtract(existing.WindowStart).TotalSeconds >= _rateLimitConfig.UserCreateEndpoint.DurationSeconds)
        {
          // 새로운 윈도우 시작
          existing.RequestCount = 1;
          existing.WindowStart = now;
        }
        else
        {
          // 기존 윈도우 내에서 카운트 증가
          existing.RequestCount++;
        }
        existing.LastRequestTime = now;
        return existing;
      });

    // Rate Limit 적용시 정보 로그 기록
    if (_rateLimitConfig.Global.LogRateLimitApplied)
    {
      var logMessage = "Rate limit applied for IP: {ClientIP}, Endpoint: {Endpoint}";
      var logParams = new List<object> { clientIp, endpoint };

      if (_rateLimitConfig.Global.IncludeRequestCountInLogs)
      {
        logMessage += ", RequestCount: {RequestCount}/{HitLimit}";
        logParams.Add(requestInfo.RequestCount);
        logParams.Add(_rateLimitConfig.UserCreateEndpoint.HitLimit);
      }

      _logger.LogInformation(logMessage, logParams.ToArray());
    }

    await Task.CompletedTask;
  }

  /// <summary>
  /// Rate Limit 응답을 사용자 정의 형식으로 처리합니다.
  /// </summary>
  /// <param name="context">HTTP 컨텍스트</param>
  /// <returns>비동기 작업</returns>
  private async Task HandleRateLimitResponse(HttpContext context)
  {
    var clientIp = GetClientIpAddress(context);
    var endpoint = context.Request.Path.ToString();

    // 현재 요청 카운터 정보 가져오기
    var requestCount = 0;
    if (_requestCounters.TryGetValue(clientIp, out var requestInfo))
    {
      requestCount = requestInfo.RequestCount;
    }

    // Rate Limit 초과 로그 기록
    if (_rateLimitConfig.Global.LogRateLimitExceeded)
    {
      var logMessage = "Rate limit exceeded for IP: {ClientIP}, Endpoint: {Endpoint}";
      var logParams = new List<object> { clientIp, endpoint };

      if (_rateLimitConfig.Global.IncludeRequestCountInLogs)
      {
        logMessage += ", RequestCount: {RequestCount}";
        logParams.Add(requestCount);
      }

      _logger.LogWarning(logMessage, logParams.ToArray());
    }

    // 사용자 정의 Rate Limit 응답 생성
    var rateLimitResponse = new RateLimitResponse
    {
      StatusCode = 429,
      Message = _rateLimitConfig.UserCreateEndpoint.ErrorMessage,
      ErrorCode = "RATE_LIMIT_EXCEEDED",
      RetryAfterSeconds = _rateLimitConfig.UserCreateEndpoint.RetryAfterSeconds,
      Details = $"분당 최대 {_rateLimitConfig.UserCreateEndpoint.HitLimit}회 요청이 허용됩니다. {_rateLimitConfig.UserCreateEndpoint.RetryAfterSeconds}초 후에 다시 시도해 주세요."
    };

    // HTTP 응답 헤더 설정
    context.Response.StatusCode = 429;
    context.Response.Headers["Retry-After"] = _rateLimitConfig.UserCreateEndpoint.RetryAfterSeconds.ToString();
    context.Response.Headers["X-RateLimit-Limit"] = _rateLimitConfig.UserCreateEndpoint.HitLimit.ToString();
    context.Response.Headers["X-RateLimit-Window"] = _rateLimitConfig.UserCreateEndpoint.DurationSeconds.ToString();
    context.Response.ContentType = "application/json";

    // 원본 응답 스트림으로 직접 JSON 응답 전송
    var jsonResponse = JsonSerializer.Serialize(rateLimitResponse, JsonOptions);
    var responseBytes = System.Text.Encoding.UTF8.GetBytes(jsonResponse);

    await context.Response.Body.WriteAsync(responseBytes, 0, responseBytes.Length);
  }

  /// <summary>
  /// 클라이언트 IP 주소를 가져옵니다.
  /// </summary>
  /// <param name="context">HTTP 컨텍스트</param>
  /// <returns>클라이언트 IP 주소</returns>
  private string GetClientIpAddress(HttpContext context)
  {
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    
    if (_rateLimitConfig.Global.IncludeClientIpInLogs)
    {
      // X-Forwarded-For 헤더 우선 확인 (프록시 환경 대응)
      var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
      if (!string.IsNullOrEmpty(forwardedFor))
      {
        // 여러 IP가 있는 경우 첫 번째 IP 사용
        clientIp = forwardedFor.Split(',')[0].Trim();
      }
    }

    return clientIp;
  }
}