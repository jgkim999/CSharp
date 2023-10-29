using DemoApplication.Exceptions;
using DemoDomain.Entities;
using MediatR;
using RestSharp;

namespace DemoApplication.Handlers;

public class RestClientRequest : IRequest<Post[]>
{
}

public class RestClientRequestHandler : IRequestHandler<RestClientRequest, Post[]>
{
    public async Task<Post[]> Handle(RestClientRequest request, CancellationToken cancellationToken)
    {
        var options = new RestClientOptions("https://jsonplaceholder.typicode.com/");
        using var restClient = new RestClient(options, useClientFactory: true);
        var response = await restClient.GetJsonAsync<Post[]>("posts");
        if (response is null)
            throw new HttpRequestError();
        return response;
    }
}