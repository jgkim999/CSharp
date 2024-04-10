using System.Net;
using DemoApplication.Exceptions;
using DemoApplication.Models;
using Microsoft.AspNetCore.Mvc;

namespace DemoWebApi.Controllers;

[Route("api/v1/[controller]/[action]")]
[ApiController]
public class ExceptionDemoController: ControllerBase
{
    [HttpGet]
    public IActionResult Local()
    {
        try
        {
            throw new NotFoundException();
        }
        catch (NotFoundException)
        {
            return NotFound(new ExceptionResponse(HttpStatusCode.NotFound, "Exception catch local"));
        }
        catch (Exception)
        {
            return BadRequest(new ExceptionResponse(HttpStatusCode.InternalServerError, "Exception catch local"));
        }
    }

    [HttpGet]
    public IActionResult Global(int id)
    {
        throw new NotFoundException();
    }
}