using System.Text.Json;
using FluentResults;
using RestSharp;

using StRunner.Models.K6;

namespace StRunner.Services;

public class K6Service
{
    private readonly ILogger<K6Service> _logger;
    private readonly RestClient _client;

    public K6Service(ILogger<K6Service> logger)
    {
        _logger = logger;
        var options = new RestClientOptions("http://localhost:6565/");
        _client = new RestClient(options, useClientFactory: true);
    }

    public async Task<Result<GetStatus>> GetStatusAsync()
    {
        try
        {
            var request = new RestRequest("v1/status", Method.Get);
            request.AddHeader("Accept", "application/json");

            var res = await _client.GetAsync<GetStatus>(request);
            if (res is null)
            {
                return Result.Fail("Failed to get k6 status");
            }
            return Result.Ok(res);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "k6 status error");
            return Result.Fail(e.Message);
        }
    }

    public async Task<Result<StopResponse>> StopAsync()
    {
        try
        {
            var statusResult = await GetStatusAsync();
            if (statusResult.IsFailed)
            {
                return Result.Fail(statusResult.Errors);
            }

            if (!statusResult.Value.Data.Attributes.Paused &&
                !statusResult.Value.Data.Attributes.Running)
            {
                return Result.Fail("k6 is not running");
            }

            var request = new RestRequest("v1/status", Method.Patch);
            request.AddHeader("Accept", "application/json");

            var stopRequest = new StopRequest()
            {
                Data = new StopData()
                {
                    Type = "status",
                    Id = statusResult.Value.Data.Id,
                    Attributes = new StopAttributes()
                    {
                        Stopped = true
                    }
                }
            };
            string jsonString = JsonSerializer.Serialize(stopRequest);

            request.AddJsonBody(jsonString);

            var res = await _client.PatchAsync<StopResponse>(request);
            if (res is null)
            {
                return Result.Fail("Failed to stop k6");
            }
            return Result.Ok(res);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "k6 stop error");
            return Result.Fail(e.Message);
        }
    }
}
