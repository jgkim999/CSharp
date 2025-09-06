using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class QueryErrorHandler : IQueryErrorHandler
{
    private readonly ILogger<QueryErrorHandler> _logger;

    public QueryErrorHandler(ILogger<QueryErrorHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleErrorAsync(IQuery query, object? result, Exception exception, CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception, "Query 처리 중 오류 발생: {QueryType}", query.GetType().Name);
        await Task.CompletedTask;
    }
}