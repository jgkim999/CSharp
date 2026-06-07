using FastEndpoints;
using System.Diagnostics;
using System.Text.Json;

namespace WebApiService.PrePostProcessor;

sealed class RequestLogger<TRequest> : IPreProcessor<TRequest>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // 제네릭 타입마다 한 번만 캐시됨
    private static readonly string RequestTypeName = typeof(TRequest).Name;

    public Task PreProcessAsync(IPreProcessorContext<TRequest> ctx, CancellationToken c)
    {
        var logger = ctx.HttpContext.Resolve<ILogger<TRequest>>();
        if (!logger.IsEnabled(LogLevel.Debug))
            return Task.CompletedTask;

        var activity = Activity.Current;
        var otelTraceId = activity?.TraceId ?? default;
        var httpTraceId = ctx.HttpContext.TraceIdentifier;

        try
        {
            var requestJson = JsonSerializer.Serialize(ctx.Request, JsonOptions);
            logger.LogDebug("TraceId: {OtelTraceId} | HttpTraceId: {HttpTraceId} | Request: {RequestType} = {RequestData}", 
                otelTraceId,
                httpTraceId,
                RequestTypeName, 
                requestJson);
        }
        catch (Exception ex)
        {
            // 파싱 실패 시 원본 데이터 출력
            logger.LogDebug("TraceId: {OtelTraceId} | HttpTraceId: {HttpTraceId} | Request: {RequestType} = {RequestData} (Serialization failed: {Error})", 
                otelTraceId,
                httpTraceId,
                RequestTypeName, 
                ctx.Request?.ToString() ?? "null", 
                ex.Message);
        }

        return Task.CompletedTask;
    }
}
