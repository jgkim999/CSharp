using DemoApplication.Handlers;
using DemoDomain.Entities;
using Flurl.Http;
using Flurl.Http.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace DemoWebApi.Controllers;

[Route("v1/api/[controller]/[action]")]
[ApiController]
public class HttpClientFactoryController : ControllerBase
{
    private readonly ILogger<HttpClientFactoryController> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly HttpClient _publicApiClient;
    private readonly HttpClient _jsonPlaceholderClient;
    private readonly IFlurlClientFactory _flurlClientFactory;
    private readonly IMediator _iMediator;
    
    public HttpClientFactoryController(
        ILogger<HttpClientFactoryController> logger,
        IMediator iMediator,
        IHttpClientFactory clientFactory,
        IFlurlClientFactory flurlClientFactory)
    {
        _logger = logger;
        _iMediator = iMediator;
        _clientFactory = clientFactory;
        _publicApiClient = clientFactory.CreateClient("PublicApis");
        _jsonPlaceholderClient = _clientFactory.CreateClient("JsonPlaceholder");
        _flurlClientFactory = flurlClientFactory;
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
        var jpResponse = await _jsonPlaceholderClient.GetAsync("posts");
        var response = JsonConvert.DeserializeObject<Post[]>(await jpResponse.Content.ReadAsStringAsync());
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> RestSharp()
    {
        // useClientFactory: Set to true if you wish to reuse the HttpClient instance
        var options = new RestClientOptions("https://jsonplaceholder.typicode.com/");
        using var restClient = new RestClient(options, useClientFactory: true);
        var response = await restClient.GetJsonAsync<Post[]>("posts");
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> Flurl()
    {
        var client = _flurlClientFactory.Get("https://jsonplaceholder.typicode.com/");
        var response = await client.Request("posts").GetJsonAsync<Post[]>();
        return Ok(response);
    }
}