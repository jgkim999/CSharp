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

public class StatusData
{
    [JsonPropertyName("attributes")]
    public Attributes Attributes { get; set; } = new();

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

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
