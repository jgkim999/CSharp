using DemoApplication.Exceptions;
using DemoDomain.Entities;
using Flurl.Http;
using Flurl.Http.Configuration;
using MediatR;

namespace DemoApplication.Handlers;

public class FlurlClientFactoryRequest : IRequest<Post[]>
{
}

public class FlurlClientFactoryRequestHandler : IRequestHandler<FlurlClientFactoryRequest, Post[]>
{
    private readonly IFlurlClientFactory _flurlClientFactory;
    
    public FlurlClientFactoryRequestHandler(IFlurlClientFactory flurlClientFactory)
    {
        _flurlClientFactory = flurlClientFactory;
    }
    
    public async Task<Post[]> Handle(FlurlClientFactoryRequest request, CancellationToken cancellationToken)
    {
        var client = _flurlClientFactory.Get("https://jsonplaceholder.typicode.com/");
        var response = await client.Request("posts").GetJsonAsync<Post[]>();
        if (response is null)
            throw new HttpRequestError();
        return response;
    }
}