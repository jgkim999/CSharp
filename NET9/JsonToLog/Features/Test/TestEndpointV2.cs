using FastEndpoints;

namespace JsonToLog.Features.Test;

public class TestEndpointV2 : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/test");
        AllowAnonymous();
        Version(2);
        Description(x => x
            .WithName("TestEndpoint V2")
            .WithSummary("A simple test endpoint V2")
            .Produces<object>(200)
            .Produces(500));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Simulate some processing
        await Task.Delay(100, ct);
        await SendOkAsync(new { Message = "Test successful V2" }, ct);
    }
}
