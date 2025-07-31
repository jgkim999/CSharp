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
    /// <param name="logger"></param>
    public ValidationErrorLogger(ILogger<ValidationErrorLogger<TRequest>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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
