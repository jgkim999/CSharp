using System.Text.Json.Serialization;

namespace StRunner.Models.K6;

public class StopData
{
    [JsonPropertyName("attributes")]
    public StopAttributes Attributes { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class StopRequest
{
    [JsonPropertyName("data")]
    public StopData Data { get; set; } = new();
}

public class StopResponse
{
    [JsonPropertyName("data")]
    public StopData Data { get; set; } = new();
}