using DemoApplication.Exceptions;
using DemoDomain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DemoApplication.Handlers;

public class PublicApiRequest : IRequest<EntryResponse>
{
}

public class PublicApiRequestHandler : IRequestHandler<PublicApiRequest, EntryResponse>
{
    private readonly ILogger<PublicApiRequestHandler> _logger;
    private readonly HttpClient _publicApiClient;
    
    public PublicApiRequestHandler(ILogger<PublicApiRequestHandler> logger, IHttpClientFactory clientFactory)
    {
        _logger = logger;
        _publicApiClient = clientFactory.CreateClient("PublicApis");
    }
    
    public async Task<EntryResponse> Handle(PublicApiRequest request, CancellationToken cancellationToken)
    {
        var publicApiResponse = await _publicApiClient.GetStringAsync("random", cancellationToken);
        var entryResponse = JsonConvert.DeserializeObject<EntryResponse>(publicApiResponse);
        if (entryResponse is null)
            throw new HttpRequestError();
        return entryResponse;
    }
}