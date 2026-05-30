using FastEndpoints;

namespace WebApiService.Endpoints.Health;

public class HealthEndpoint : Endpoint<EmptyRequest>
{
    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        await Send.ResponseAsync(new { Status = "Healthy" }, cancellation: ct);
    }
}
