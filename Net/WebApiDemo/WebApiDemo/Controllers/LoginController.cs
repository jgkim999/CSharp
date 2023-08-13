using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WebApiApplication.Exceptions;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiDemo.Controllers;

[Route("api/v1/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly ILogger<LoginController> _logger;
    private readonly IAccountService _accountService;

    public LoginController(ILogger<LoginController> logger, IAccountService accountService)
    {
        _logger = logger;
        _accountService = accountService;
    }

    // GET: api/<LoginController>
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    // GET api/<LoginController>/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/<LoginController>    
    [HttpPost]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(string))]
    public async Task<IActionResult> Post([FromBody] LoginReq req)
    {
        try
        {
            AccountDto dto = await _accountService.LoginAsync(req);
            return Ok(dto);
        }
        catch (CreateAccountFailedException ex)
        {
            return NotFound(ex);
        }
    }

    // PUT api/<LoginController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<LoginController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}
