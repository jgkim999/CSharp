using FastEndpoints;
using System.Diagnostics;
using System.Text.Json;
using WebApiService.Endpoints.Account.Login;

namespace WebApiService.PrePostProcessor;

sealed class ResponseLogger : IPostProcessor<Request, Response>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public Task PostProcessAsync(IPostProcessorContext<Request, Response> ctx, CancellationToken ct)
    {
        var logger = ctx.HttpContext.Resolve<ILogger<Response>>();
        if (!logger.IsEnabled(LogLevel.Debug))
            return Task.CompletedTask;

        var otelTraceId = Activity.Current?.TraceId;
        var httpTraceId = ctx.HttpContext.TraceIdentifier;

        try
        {
            var requestJson = JsonSerializer.Serialize(ctx.Request, JsonOptions);
            var responseJson = JsonSerializer.Serialize(ctx.Response, JsonOptions);
            logger.LogDebug("TraceId: {OtelTraceId} | HttpTraceId: {HttpTraceId} | Request: {RequestData}, Response: {ResponseData}", 
                otelTraceId,
                httpTraceId,
                requestJson, 
                responseJson);
        }
        catch (Exception ex)
        {
            // 파싱 실패 시 원본 데이터 출력
            logger.LogDebug("TraceId: {OtelTraceId} | HttpTraceId: {HttpTraceId} | Request: {RequestData}, Response: {ResponseData} (Serialization failed: {Error})", 
                otelTraceId,
                httpTraceId,
                ctx.Request?.ToString() ?? "null", 
                ctx.Response?.ToString() ?? "null", 
                ex.Message);
        }

        return Task.CompletedTask;
    }
}
