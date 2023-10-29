using System.Globalization;
using MassTransit.Mediator;
using MediatrDemo.Handler;
using MediatrDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace MediatrDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    private readonly ILogger<PingController> _logger;
    private readonly IMediator _mediator;
    
    public PingController(IMediator mediator, ILogger<PingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    [HttpGet(Name = "GetPing")]
    public async Task<PongRes> Get()
    {
        var response = await _mediator.Send<Ping>(new Ping());
        _logger.LogInformation(response.ToString(CultureInfo.CurrentCulture)); // "Pong"
        var res = new PongRes();
        res.At = response;
        return res;
    }
}