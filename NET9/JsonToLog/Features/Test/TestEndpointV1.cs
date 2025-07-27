using FastEndpoints;

namespace JsonToLog.Features.Test;

public class TestEndpointV1 : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/test");
        AllowAnonymous();
        Version(1);
        Description(x => x
            .WithName("TestEndpoint V1")
            .WithSummary("A simple test endpoint")
            .Produces<object>(200)
            .Produces(500));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Simulate some processing
        await Task.Delay(100, ct);
        await SendOkAsync(new { Message = "Test successful V1" }, ct);
    }
}
