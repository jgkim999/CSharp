using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace WebApiDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class UlidController : ControllerBase
{
    /// <summary>
    /// Ulid 고유키 요청
    /// </summary>
    /// <returns>생성한 고유키</returns>
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(string))]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests)]
    public IActionResult Get()
    {
        return Ok(Ulid.NewUlid());
    }

    [Route("{id}")]
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests)]
    public IActionResult Get(int id)
    {
        List<string> ulids = new();
        for (int i = 0; i < id; ++i)
        {
            ulids.Add(Ulid.NewUlid().ToString());
        }
        return Ok(ulids);
    }
}
