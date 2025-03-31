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

    public async Task<Result> StartAsync(string jsName)
    {
        try
        {
            if (!File.Exists($"/app/k6/{jsName}"))
            {
                return Result.Fail($"File {jsName} not found");
            }

            await StopAsync();

            Task.Run(() =>
            {
                string command = string.Format($"K6_WEB_DASHBOARD=true /app/k6/k6 run -o experimental-prometheus-rw /app/k6/{jsName}", jsName);
                
                string result = "";

                using System.Diagnostics.Process k6Process = new System.Diagnostics.Process();
                
                k6Process.StartInfo.FileName = "/bin/bash";
                k6Process.StartInfo.Arguments = "-c \" " + command + " \"";
                k6Process.StartInfo.UseShellExecute = false;
                k6Process.StartInfo.RedirectStandardOutput = true;
                k6Process.StartInfo.RedirectStandardError = true;
                k6Process.Start();

                while (!k6Process.StandardOutput.EndOfStream)
                {
                    string? line = k6Process.StandardOutput.ReadLine();
                    if (line != null)
                        _logger.LogInformation(line);
                }
                /*
                    ProcessStream processStream = new ProcessStream();
                    try
                    {
                        processStream.Read(proc);

                        proc.WaitForExit();
                        processStream.Stop();
                        if (!proc.HasExited)
                        {
                            // OK, we waited until the timeout but it still didn't exit; just kill the process now
                            //timedOut = true;
                            try
                            {
                                proc.Kill();
                                processStream.Stop();
                            }
                            catch
                            {
                            }
                            proc.WaitForExit();
                        }
                    }
                    catch (Exception ex)
                    {
                        proc.Kill();
                        processStream.Stop();
                        throw ex;
                    }
                    finally
                    {
                        processStream.Stop();
                    }
                    */
                //result += proc.StandardOutput.ReadToEnd();
                //result += proc.StandardError.ReadToEnd();
                k6Process.WaitForExit();
                _logger.LogInformation("Exit code: {ExitCode}", k6Process.ExitCode);

                return result;
            });

            return Result.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to stop k6");
            return Result.Fail(e.Message);
        }
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
