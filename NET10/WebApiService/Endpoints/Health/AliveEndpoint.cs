using FastEndpoints;

namespace WebApiService.Endpoints.Health;

public class AliveEndpoint : Endpoint<EmptyRequest>
{
    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/alive");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        await Send.ResponseAsync(new { Status = "alive" }, cancellation: ct);
    }
}
