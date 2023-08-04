using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using Swashbuckle.AspNetCore.Annotations;

namespace WebApiDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("fixed")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class RateLimitController : ControllerBase
    {
        /// <summary>
        /// 요청 제한 예제입니다.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        [SwaggerResponse(StatusCodes.Status429TooManyRequests)]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
    }
}
