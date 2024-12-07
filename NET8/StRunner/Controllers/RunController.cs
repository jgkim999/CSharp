using Microsoft.AspNetCore.Mvc;

using RestSharp;

using System.Threading;
using StRunner.Models.K6;
using StRunner.Services;
using StRunner.Utils;

namespace StRunner.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RunController : ControllerBase
    {
        private readonly ILogger<RunController> _logger;
        private readonly K6Service _k6Service;

        public RunController(ILogger<RunController> logger, K6Service k6Service)
        {
            _logger = logger;
            _k6Service = k6Service;
        }

        [HttpGet]
        public async Task<IActionResult> RunAsync()
        {
            Task.Run(() =>
            {
                string command = "K6_WEB_DASHBOARD=true /app/k6/k6 run -o experimental-prometheus-rw /app/k6/test.js";
                string result = "";
                using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
                {
                    proc.StartInfo.FileName = "/bin/bash";
                    proc.StartInfo.Arguments = "-c \" " + command + " \"";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.Start();

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadLine();
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
                    proc.WaitForExit();
                    _logger.LogInformation("Exit code: {ExitCode}", proc.ExitCode);
                }

                return result;
            });
            await Task.CompletedTask;
            return Ok("Run");
        }

        [HttpGet]
        public async Task<IActionResult> StatusAsync()
        {
            try
            {
                var result = await _k6Service.GetStatusAsync();
                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                return BadRequest(result.Reasons.Select(reason => reason.Message));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "k6 status error");
                return BadRequest();
            }
        }

        [HttpPatch]
        public async Task<IActionResult> StopAsync()
        {
            try
            {
                var result = await _k6Service.StopAsync();
                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }
                return BadRequest(result.Reasons.Select(reason => reason.Message));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "k6 status error");
                return BadRequest();
            }
        }
    }
}
