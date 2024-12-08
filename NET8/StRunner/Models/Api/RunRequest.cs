using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StRunner.Models.Api;

public class RunRequest
{
    [Required]
    [JsonPropertyName("jsName")]
    public string JsName { get; set; } = string.Empty;
}
