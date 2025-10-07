using System.ComponentModel;
using MessagePack;

namespace Demo.Application.DTO;

/// <summary>
/// </summary>
[MessagePackObject]
public class MqPublishResponse
{
    /// <summary>
    /// </summary>
    [Key(0)]
    [DefaultValue("Hello MQ")]
    public string Message { get; set; } = "Hello MQ";
}
