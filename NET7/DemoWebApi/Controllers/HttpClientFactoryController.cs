using DemoApplication.Handlers;
using Flurl.Http.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DemoWebApi.Controllers;

[Route("v1/api/[controller]/[action]")]
[ApiController]
public class HttpClientFactoryController : ControllerBase
{
    private readonly IMediator _iMediator;
    
    public HttpClientFactoryController(IMediator iMediator)
    {
        _iMediator = iMediator;
    }
    
    [HttpGet]
    public async Task<IActionResult> PublicApis()
    {
        var response = await _iMediator.Send(new PublicApiRequest());
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> JsonPlaceHolder()
    {
        var response = await _iMediator.Send(new JsonPlaceholderClientRequest());
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> RestSharp()
    {
        var response = await _iMediator.Send(new RestClientRequest());
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> Flurl()
    {
        var response = await _iMediator.Send(new FlurlClientFactoryRequest());
        return Ok(response);
    }
}