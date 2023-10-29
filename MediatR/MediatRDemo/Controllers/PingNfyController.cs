using MediatR;
using MediatrDemo.Handler;
using MediatrDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace MediatrDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PingNfyController : ControllerBase
{
    private readonly ILogger<PingNfyController> _logger;
    private readonly IMediator _mediator;
        
    public PingNfyController(ILogger<PingNfyController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }
        
    [HttpGet(Name = "PingNfy")]
    public async Task<PongRes> Get()
    {
        await _mediator.Publish(new PingNfy());
        _logger.LogInformation("PingNfy"); // "Pong"
        var res = new PongRes()
        {
            At = DateTime.Now
        };
        return res;
    }
}