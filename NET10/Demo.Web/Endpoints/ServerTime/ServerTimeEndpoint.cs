using System.Diagnostics;
using Demo.Application.Extensions;
using Demo.Application.Handlers.Queries;
using Demo.Application.Models;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Queries.Abstractions;

namespace Demo.Web.Endpoints.ServerTime;

public class ServerTimeSummary : Summary<ServerTimeEndpoint>
{
    public ServerTimeSummary()
    {
        Summary = "서버 시간";
        Description = "NodaTime Example";
    }
}

/// <summary>
/// 서버 시간을 반환
/// </summary>
public class ServerTimeEndpoint : EndpointWithoutRequest
{
    private readonly ILogger<ServerTimeEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;
    
    /// <summary>
    /// 서버 시간 반환
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="telemetryService"></param>
    public ServerTimeEndpoint(ILogger<ServerTimeEndpoint> logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }
    
    /// <summary>
    /// 엔드포인트 구성
    /// </summary>
    public override void Configure()
    {
        Get("/api/serverTime");
        AllowAnonymous();
        //Version(1);
        Group<ServerTimeGroup>();
        Summary(new ServerTimeSummary());
    }

    /// <summary>
    /// Handles the HTTP GET request for the logging test endpoint, performing logging and telemetry operations, simulating nested activities and error handling, and returning trace information in the response.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity? parentActivity = Activity.Current;
        using Activity? span = _telemetryService.StartActivity(nameof(ServerTimeEndpoint), ActivityKind.Internal, parentActivity?.Context);
        
        _logger.LogInfoWithCaller("ServerTimeEndpoint {Date}", DateTime.UtcNow);
        
        var queryMediator = Resolve<IQueryMediator>();
        var query = new ServerTimeQuery();
        var result = await queryMediator.QueryAsync(query, ct);
        Response = new ServerTimeResponse()
        {
            Utc = result.utc,
            Korea = result.korea,
            KoreanCalendar = result.koreanCalendar
        };
    }
}
