using JsonToLog.Configs;
using JsonToLog.Services;

using OpenSearch.Net;

using OpenTelemetry;

using System.Diagnostics;
using System.Text.Json.Nodes;

namespace JsonToLog.Features.LogSend;

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

            JsonObject jObj = new();
            foreach (var (key, value) in task.LogData)
            {
                if (value is string)
                    jObj[key] = value.ToString();
                else if (value is int i)
                    jObj[key] = i;
                else if (value is double d)
                    jObj[key] = d;
                else if (value is long l)
                    jObj[key] = l;
                else if (value is float f)
                    jObj[key] = f;
                else if (value is bool b)
                    jObj[key] = b;
                else if (value is DateTime dt)
                    jObj[key] = dt.ToString("o"); // ISO 8601 format
                else
                    jObj[key] = value?.ToString() ?? "null"; // Handle nulls and other types)
            }
            jObj["ts"] = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            string json = System.Text.Json.JsonSerializer.Serialize(jObj);
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
