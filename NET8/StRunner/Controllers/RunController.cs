using Microsoft.AspNetCore.Mvc;

using RestSharp;

namespace StRunner.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RunController : ControllerBase
    {
        private readonly ILogger<RunController> _logger;
        private readonly RestClient _client;

        public RunController(ILogger<RunController> logger)
        {
            _logger = logger;
            var options = new RestClientOptions("http://localhost:6565/");
            _client = new RestClient(options, useClientFactory: true);
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

                    result += proc.StandardOutput.ReadToEnd();
                    result += proc.StandardError.ReadToEnd();

                    proc.WaitForExit();
                }

                return result;
            });
            await Task.CompletedTask;
            return Ok("Run");
        }

        [HttpGet]
        public async Task<IActionResult> StatusAsync()
        {
            var request = new RestRequest("v1/status", Method.Get);
            request.AddHeader("Accept", "application/json");

            var res = await _client.GetAsync(request);
            return Ok(res.Content);
        }
    }
}
