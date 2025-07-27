using FastEndpoints;

using Geohash;

using JsonToLog.Features.LogSend;
using JsonToLog.Models;
using JsonToLog.Services;

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

using System.Diagnostics;
using System.Text.Json.Nodes;

namespace JsonToLog.Features.Fake;

public class FakeEndpointV1 : EndpointWithoutRequest
{
    private readonly ILogger<FakeEndpointV1> _logger;
    private readonly LogSendProcessor _logSendProcessor;
    private readonly IP2LocationService _locationService;
    
    public FakeEndpointV1(
        ILogger<FakeEndpointV1> logger,
        LogSendProcessor logSendProcessor,
        IP2LocationService ip2LocationService)
    {
        _logger = logger;
        _logSendProcessor = logSendProcessor;
        _locationService = ip2LocationService;
    }
    
    public override void Configure()
    {
        Get("/api/fake");
        AllowAnonymous();
        Version(1);
        Description(x => x
            .WithName("FakeEndpoint V1")
            .WithSummary("A simple test endpoint")
            .Produces<object>(200)
            .Produces(500));
    }
    
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivityService.StartActivity("FakeEndpointV1.HandleAsync", ActivityKind.Producer);

        //var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        int count = Query<int>("Count");
        
        _logger.LogInformation("Received count request: {Count}", count);

        var fake = new Bogus.Faker();
        
        for (int i = 0; i < count; ++i)
        {
            var ip = fake.Internet.IpAddress().ToString();
            var country = _locationService.GetCountry(ip);
            var version = $"{fake.Random.Number(3)}.{fake.Random.Number(3)}.{fake.Random.Number(3)}";
            var ts = DateTime.UtcNow.AddHours(-fake.Random.Number(0, 168)).ToString("o"); // ISO 8601 format
            var lat = fake.Address.Latitude();
            var lon = fake.Address.Longitude();
            JsonObject jsonObject = new JsonObject();
            jsonObject["lat"] = lat;
            jsonObject["lon"] = lon;
            
            var geohasher = new Geohasher();
            string geohash = geohasher.Encode(lat, lon, 9); // 9 character precision
            
            // Send a log message
            await _logSendProcessor.SendLogAsync(new LogSendTask()
            {
                LogData = new Dictionary<string, object>
                {
                    { "country", country },
                    { "ip", ip },
                    { "ping", fake.Random.Int(1, 300) },
                    { "version", version },
                    { "geo.coordinates", jsonObject.ToJsonString() },
                    { "geo.geohash", geohash },
                    { "geo.lat", lat },
                    { "geo.lon", lon },
                    { "ts", ts },
                },
                PropagationContext = new PropagationContext(
                    activity?.Context ?? default,
                    Baggage.Current)
            });
        }
        await SendOkAsync(new { Message = "Fake endpoint processed successfully" }, cancellation: cancellationToken);
    }
}