using Demo.Application.WeatherForecast;
using FastEndpoints;
using LiteBus.Commands.Abstractions;

namespace WebApiService.Endpoints.WeatherForecast;

public class WeatherForecastEndpoint : Endpoint<EmptyRequest, WeatherForecastRes[]>
{
    private ICommandMediator _command;
    
    public WeatherForecastEndpoint(ICommandMediator command)
    {
        _command = command;
    }

    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/wf");
        AllowAnonymous();

        Description(b => b.Produces(403));
        Summary(s => {
            s.Summary = "short summary goes here";
            s.Description = "long description goes here";
            //s.ExampleRequest = new MyRequest { ...};
            //s.ResponseExamples[200] = new MyResponse { ...};
            s.Responses[200] = "ok response description goes here";
            s.Responses[403] = "forbidden response description goes here";
        });
    }
    
    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var forecasts = await _command.SendAsync(new WeatherForecastCommand(), ct);
        await Send.ResponseAsync(forecasts, cancellation: ct);
    }
}
