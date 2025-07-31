using FastEndpoints;

namespace GamePulse.Processors;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public class ValidationErrorLogger<TRequest> : IPreProcessor<TRequest>
{
    private readonly ILogger<ValidationErrorLogger<TRequest>> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrorLogger{TRequest}"/> class.
    /// </summary>
    public ValidationErrorLogger(ILogger<ValidationErrorLogger<TRequest>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <summary>
    /// Logs validation errors for the given request if any are present in the context.
    /// </summary>
    /// <param name="context">The pre-processor context containing the request and validation results.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        if (context.ValidationFailures.Count > 0)
        {
            _logger.LogWarning("Validation failed for {RequestType}: {Errors}", 
                typeof(TRequest).Name,
                string.Join(", ", context.ValidationFailures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));
        }

        return Task.CompletedTask;
    }
}
