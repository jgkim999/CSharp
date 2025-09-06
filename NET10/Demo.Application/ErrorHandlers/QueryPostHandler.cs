using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class QueryPostHandler : IQueryPostHandler
{
    private readonly ILogger<QueryPostHandler> _logger;

    public QueryPostHandler(ILogger<QueryPostHandler> logger)
    {
        _logger = logger;
    }

    public async Task PostHandleAsync(IQuery query, object? result, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Query 처리 완료: {QueryType}", query.GetType().Name);
        await Task.CompletedTask;
    }
}