using System.Text.Json.Serialization;

namespace StRunner.Models.K6;

public class Attributes
{
    [JsonPropertyName("paused")]
    public bool Paused { get; set; } = false;

    [JsonPropertyName("running")]
    public bool Running { get; set; } = false;

    [JsonPropertyName("tainted")]
    public bool Tainted { get; set; } = false;

    [JsonPropertyName("vus")]
    public int Vus { get; set; } = 0;
}
