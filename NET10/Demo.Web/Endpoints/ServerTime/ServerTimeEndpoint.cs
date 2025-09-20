using System.Diagnostics;
using Demo.Application.Extensions;
using Demo.Application.Models;
using Demo.Application.Queries;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Queries.Abstractions;

namespace Demo.Web.Endpoints.ServerTime;

public class ServerTimeEndpoint : EndpointWithoutRequest
{
    private readonly ILogger<ServerTimeEndpoint> _logger;
    private readonly ITelemetryService _telemetryService;
    
    public ServerTimeEndpoint(ILogger<ServerTimeEndpoint> logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }
    
    public override void Configure()
    {
        Get("/api/serverTime");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "서버 시간";
            s.Description = "NodaTime Example";
        });
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
