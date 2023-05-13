using System.Diagnostics;
using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers;
[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    private ILogger<PingController> _logger;
    private readonly IMediator _mediator;
    
    public PingController(IMediator mediator, ILogger<PingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    [HttpGet(Name = "GetPing")]
    public async Task<PongRes> Get()
    {
        var response = await _mediator.Send(new Ping.Ping());
        _logger.LogInformation(response.ToString(CultureInfo.CurrentCulture)); // "Pong"
        var res = new PongRes();
        res.At = response;
        return res;
    }
}