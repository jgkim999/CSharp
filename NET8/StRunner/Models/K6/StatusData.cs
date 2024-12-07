using System.Text.Json.Serialization;

namespace StRunner.Models.K6;

public class StatusData
{
    [JsonPropertyName("attributes")]
    public Attributes Attributes { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}