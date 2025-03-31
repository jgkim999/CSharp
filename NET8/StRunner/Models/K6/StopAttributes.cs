using System.Text.Json.Serialization;

namespace StRunner.Models.K6;

public class StopAttributes
{
    [JsonPropertyName("stopped")]
    public bool Stopped { get; set; } = false;
}
