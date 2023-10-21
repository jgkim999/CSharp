using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DemoApplication.Exceptions;
using DemoApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GlobalExceptionDemo.Controllers
{
    [Route("api/v1/[controller]/[action]")]
    [ApiController]
    public class ExceptionDemoController : ControllerBase
    {
        [HttpGet]
        public IActionResult Local()
        {
            try
            {
                throw new NotFoundException();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ExceptionResponse(HttpStatusCode.NotFound, "Exception catch local"));
            }
            catch (Exception ex)
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
}
