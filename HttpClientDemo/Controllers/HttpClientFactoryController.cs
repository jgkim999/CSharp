using Flurl.Http;
using Flurl.Http.Configuration;
using HttpClientDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace HttpClientDemo.Controllers;

[ApiController]
[Route("[controller]/[action]")]
[Produces("application/json")]
public class HttpClientFactoryController : ControllerBase
{
    private readonly ILogger<HttpClientFactoryController> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly HttpClient _publicApiClient;
    private readonly HttpClient _jsonPlaceholderClient;
    private readonly IFlurlClientFactory _flurlClientFactory;
    
    public HttpClientFactoryController(
        ILogger<HttpClientFactoryController> logger,
        IHttpClientFactory clientFactory,
        IFlurlClientFactory flurlClientFactory)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _publicApiClient = clientFactory.CreateClient("PublicApis");
        _jsonPlaceholderClient = _clientFactory.CreateClient("JsonPlaceholder");
        _flurlClientFactory = flurlClientFactory;
    }
    
    [HttpGet]
    public async Task<IActionResult> PublicApis()
    {
        var publicApiResponse = await _publicApiClient.GetStringAsync("random");
        var entryResponse = JsonConvert.DeserializeObject<EntryResponse>(publicApiResponse);
        return Ok(entryResponse);
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