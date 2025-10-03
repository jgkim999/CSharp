using MemoryPack;

namespace Demo.Application.Models;

[MemoryPackable]
public partial class MemoryPackRequest
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}