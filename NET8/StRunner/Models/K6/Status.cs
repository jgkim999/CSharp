using System.Text.Json.Serialization;

namespace StRunner.Models.K6;

public class GetStatus
{
    [JsonPropertyName("data")]
    public StatusData Data { get; set; } = new();
}

public class UpdateStatus
{
    [JsonPropertyName("data")]
    public StatusData Data { get; set; } = new();
}
