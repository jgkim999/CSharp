using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiDemo.Controllers;

[Route("api/v1/[controller]/[action]")]
[Consumes("application/json")]
[Produces("application/json")]
[ApiController]
public class GameItemAddController : ControllerBase
{
    private readonly ILogger<GameItemAddController> _logger;
    private readonly ISessionService _sessionService;
    private readonly IGameService _gameService;

    public GameItemAddController(
        ILogger<GameItemAddController> logger,
        ISessionService sessionService,
        IGameService gameService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _gameService = gameService;
    }

    [HttpPost]
    public async Task<IActionResult> AddOne([FromBody] ReqBase req)
    {
        var (success, userId) = await _sessionService.IsValid(req.SessionId);
        if (success == false)
        {
            return Ok(new ResBase() { ErrorCode = 1 });
        }
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < 100; ++i)
        {
            await _gameService.AddOneAsync(userId);
        }
        sw.Stop();
        var elsp = sw.ElapsedMilliseconds;
        // 555msec
        return Ok(elsp);
    }

    [HttpPost]
    public async Task<IActionResult> AddMulti([FromBody] ReqBase req)
    {
        var (success, userId) = await _sessionService.IsValid(req.SessionId);
        if (success == false)
        {
            return Ok(new ResBase() { ErrorCode = 1 });
        }
        var sw = new Stopwatch();
        sw.Start();
        await _gameService.AddAsync(userId, 100);
        sw.Stop();
        // 8msec
        var elsp = sw.ElapsedMilliseconds;
        return Ok(elsp);
    }
}
