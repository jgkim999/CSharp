using Microsoft.AspNetCore.Mvc;

using StRunner.Models.Api;
using StRunner.Services;

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
            var result = await _k6Service.StartAsync("test.js");
            if (!result.IsSuccess)
            {
                return BadRequest(result.Reasons.Select(reason => reason.Message));
            }
            return Ok("Run");
        }

        [HttpPost]
        public async Task<IActionResult> RunAsync([FromBody] RunRequest req)
        {
            if (string.IsNullOrEmpty(req.JsName))
            {
                return BadRequest("JsName is required");
            }

            var result = await _k6Service.StartAsync(req.JsName);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Reasons.Select(reason => reason.Message));
            }
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

        [HttpGet]
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
