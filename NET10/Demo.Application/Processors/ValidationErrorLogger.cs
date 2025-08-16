using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Processors;

/// <summary>
/// 요청 유효성 검사 오류를 로깅하는 전처리기
/// </summary>
/// <typeparam name="TRequest">요청 타입</typeparam>
public class ValidationErrorLogger<TRequest> : IPreProcessor<TRequest>
{
    private readonly ILogger<ValidationErrorLogger<TRequest>> _logger;

    /// <summary>
    /// ValidationErrorLogger 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrorLogger{TRequest}"/> class.
    /// </summary>
    /// <remarks>
    /// Stores a logger used to record validation failures for the request type.
    /// </remarks>
    public ValidationErrorLogger(ILogger<ValidationErrorLogger<TRequest>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 주어진 요청에 대한 유효성 검사 오류가 컨텍스트에 있는 경우 이를 로깅합니다
    /// </summary>
    /// <param name="context">요청과 유효성 검사 결과를 포함하는 전처리기 컨텍스트</param>
    /// <param name="ct">취소 토큰</param>
    /// <summary>
    /// Pre-processes a request by logging validation failures, if any.
    /// </summary>
    /// <param name="context">Pre-processor context containing validation failures for the request.</param>
    /// <param name="ct">Cancellation token (not observed by this implementation).</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        if (context.ValidationFailures.Count > 0)
        {
            _logger.LogWarning("유효성 검사 실패 - 요청 타입: {RequestType}, 오류: {Errors}",
                typeof(TRequest).Name,
                string.Join(", ", context.ValidationFailures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));
        }

        return Task.CompletedTask;
    }
}