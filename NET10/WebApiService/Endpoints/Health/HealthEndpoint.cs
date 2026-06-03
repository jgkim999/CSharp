using FastEndpoints;

namespace WebApiService.Endpoints.Health;

public class HealthEndpoint : Endpoint<EmptyRequest>
{
    private readonly ILogger<HealthEndpoint> _logger;
    
    public HealthEndpoint(ILogger<HealthEndpoint> logger)
    {
        _logger = logger;
    }
    
    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        try
        {
            await Send.ResponseAsync(new { Status = "Healthy" }, cancellation: ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // 클라이언트가 요청을 취소했음 — 필요시 디버그/트레이스만 남김
            // 예: LogDebug("Request cancelled by client.");
            _logger.LogDebug("Request cancelled by client");
        }
        catch (Exception ex)
        {
            // 예외 처리 — 필요시 로깅
            _logger.LogError(ex, "Error processing health check request");
            await Send.ResponseAsync(new { Status = "Error", Message = ex.Message }, cancellation: ct);
        }
    }
}
