namespace JsonToLog.Configs;

public class OpenSearchConfig
{
    public string Endpoint { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "game-logs";
}
