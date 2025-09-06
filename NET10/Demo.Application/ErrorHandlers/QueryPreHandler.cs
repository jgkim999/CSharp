using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class QueryPreHandler : IQueryPreHandler
{
    private readonly ILogger<QueryPreHandler> _logger;

    public QueryPreHandler(ILogger<QueryPreHandler> logger)
    {
        _logger = logger;
    }

    public async Task PreHandleAsync(IQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Query 처리 시작: {QueryType}", query.GetType().Name);
        await Task.CompletedTask;
    }
}