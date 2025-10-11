using MemoryPack;
using MessagePack;
using ProtoBuf;

namespace Demo.Zen;

[MessagePackObject]
[ProtoContract]
[MemoryPackable]
public partial class SampleModel
{
    [Key(0)]
    [ProtoMember(1)]
    public int Id { get; set; }

    [Key(1)]
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [Key(2)]
    [ProtoMember(3)]
    public double Value { get; set; }
}
