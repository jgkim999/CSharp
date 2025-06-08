using JsonToLog.Configs;
using JsonToLog.Metrics;
using JsonToLog.Models;
using JsonToLog.Services;
using JsonToLog.Utils;

using OpenSearch.Net;

using OpenTelemetry;

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonToLog.Repositories;

public class LogRepositoryOpenSearch : ILogRepository
{
    private readonly ILogger<LogRepositoryOpenSearch> _logger;
    private readonly LogSendMetrics _logSendMetrics;
    private readonly OpenSearchLowLevelClient _client;
    private readonly string _indexName;
    
    public LogRepositoryOpenSearch(
        ILogger<LogRepositoryOpenSearch> logger,
        OpenSearchConfig openSearchConfig,
        LogSendMetrics logSendMetrics)
    {
        _logger = logger;
        _logSendMetrics = logSendMetrics;
        _indexName = openSearchConfig.IndexName;
        
        var nodeAddress = new Uri(openSearchConfig.Endpoint);
        var connectionPool = new SingleNodeConnectionPool(nodeAddress);
        var settings = new ConnectionConfiguration(connectionPool)
            .RequestTimeout(TimeSpan.FromSeconds(30))
            .MaximumRetries(3)
            .EnableHttpCompression()
            .ThrowExceptions()
            .PrettyJson();
        _client = new OpenSearchLowLevelClient(settings);
    }

    public async Task<bool> SendLogAsync(LogSendTask task)
    {
        try
        {
            Baggage.Current = task.PropagationContext.Baggage;
            using var activity = ActivityService.StartActivity("LogRepository.OpenSearch", ActivityKind.Internal, task.PropagationContext.ActivityContext);
            string indexName = $"{_indexName}-{DateTime.UtcNow:yyyyMMddHH}";
            
            JsonObject jObj = JsonProcessor.ConvertToJsonObject(task.LogData);
            string json = jObj.ToJsonString();
            
            await _client.IndexAsync<StringResponse>(
                indexName,
                Ulid.NewUlid().ToString(),
                PostData.String(json));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send log data: {@LogData}", task.LogData);
            _logSendMetrics.RecordLogSendFailed();
            return false;
        }
    }
}
