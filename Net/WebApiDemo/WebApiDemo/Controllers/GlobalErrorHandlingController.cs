using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace WebApiDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class GlobalErrorHandlingController : ControllerBase
{
    private readonly ILogger<GlobalErrorHandlingController> _logger;

    public GlobalErrorHandlingController(ILogger<GlobalErrorHandlingController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET 
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
    public IActionResult Get()
    {
        _logger.LogInformation("Fetching all values");
        return Ok(new string[] { "value1", "value2" });
    }

    /// <summary>
    /// GlobalException 테스트 입니다.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="AccessViolationException"></exception>
    [HttpGet("{id}")]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError)]
    public IActionResult Get(int id)
    {
        _logger.LogInformation($"Fetching value {id}");
        throw new AccessViolationException("Violation Exception while accessing the resource.");
        //return Ok(new string[] { "value1", "value2" });
    }
}
