using MessagePack;

namespace Demo.SimpleSocketShare.Messages;

[MessagePackObject]
public class VeryLongRes
{
    [Key(0)]
    public string Data { get; set; } = string.Empty;
}
