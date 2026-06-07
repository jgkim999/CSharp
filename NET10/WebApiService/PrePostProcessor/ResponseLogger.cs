using FastEndpoints;
using System.Diagnostics;
using System.Text.Json;

namespace WebApiService.PrePostProcessor;

sealed class ResponseLogger<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string RequestTypeName = typeof(TRequest).Name;
    private static readonly string ResponseTypeName = typeof(TResponse).Name;

    public Task PostProcessAsync(IPostProcessorContext<TRequest, TResponse> ctx, CancellationToken ct)
    {
        var logger = ctx.HttpContext.Resolve<ILogger<TResponse>>();
        if (!logger.IsEnabled(LogLevel.Debug))
            return Task.CompletedTask;

        var activity = Activity.Current;
        var otelTraceId = activity?.TraceId ?? default;
        var httpTraceId = ctx.HttpContext.TraceIdentifier;

        try
        {
            var requestJson = JsonSerializer.Serialize(ctx.Request, JsonOptions);
            var responseJson = JsonSerializer.Serialize(ctx.Response, JsonOptions);
            logger.LogDebug("TraceId: {OtelTraceId} | HttpTraceId: {HttpTraceId} | Request: {RequestType} = {RequestData}, Response: {ResponseType} = {ResponseData}", 
                otelTraceId,
                httpTraceId,
                RequestTypeName,
                requestJson,
                ResponseTypeName, 
                responseJson);
        }
        catch (Exception ex)
        {
            // 파싱 실패 시 원본 데이터 출력
            logger.LogDebug("TraceId: {OtelTraceId} | HttpTraceId: {HttpTraceId} | Request: {RequestType} = {RequestData}, Response: {ResponseType} = {ResponseData} (Serialization failed: {Error})", 
                otelTraceId,
                httpTraceId,
                RequestTypeName,
                ctx.Request?.ToString() ?? "null",
                ResponseTypeName, 
                ctx.Response?.ToString() ?? "null", 
                ex.Message);
        }

        return Task.CompletedTask;
    }
}
