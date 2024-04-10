using DemoApplication.Exceptions;
using DemoDomain.Entities;
using MediatR;
using Newtonsoft.Json;

namespace DemoApplication.Handlers;

public class JsonPlaceholderClientRequest : IRequest<Post[]>
{
}

public class JsonPlaceholderClientRequestHandler : IRequestHandler<JsonPlaceholderClientRequest, Post[]>
{
    private readonly HttpClient _jsonPlaceholderClient;
    
    public JsonPlaceholderClientRequestHandler(IHttpClientFactory clientFactory)
    {
        _jsonPlaceholderClient = clientFactory.CreateClient("JsonPlaceholder");
    }
    
    public async Task<Post[]> Handle(JsonPlaceholderClientRequest request, CancellationToken cancellationToken)
    {
        var jpResponse = await _jsonPlaceholderClient.GetAsync("posts");
        var response = JsonConvert.DeserializeObject<Post[]>(await jpResponse.Content.ReadAsStringAsync());
        if (response is null)
            throw new HttpRequestError();
        return response;
    }
}