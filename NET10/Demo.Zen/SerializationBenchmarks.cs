using BenchmarkDotNet.Attributes;
using MessagePack;
using MemoryPack;

namespace Demo.Zen;

[MarkdownExporter] 
[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private SampleModel? _model;
    private byte[]? _messagePackBytes;
    private byte[]? _protoBufBytes;
    private byte[]? _memoryPackBytes;

    [GlobalSetup]
    public void Setup()
    {
        _model = new SampleModel { Id = 1, Name = "테스트", Value = 123.456 };
        _messagePackBytes = MessagePackSerializer.Serialize(_model);
        using var ms = new MemoryStream();
        ProtoBuf.Serializer.Serialize(ms, _model);
        _protoBufBytes = ms.ToArray();
        _memoryPackBytes = MemoryPackSerializer.Serialize(_model);
    }

    [Benchmark]
    public byte[] MessagePack_Serialize() => MessagePackSerializer.Serialize(_model);

    [Benchmark]
    public SampleModel MessagePack_Deserialize() => MessagePackSerializer.Deserialize<SampleModel>(_messagePackBytes);

    [Benchmark]
    public byte[] ProtoBuf_Serialize()
    {
        using var ms = new MemoryStream();
        ProtoBuf.Serializer.Serialize(ms, _model);
        return ms.ToArray();
    }

    [Benchmark]
    public SampleModel ProtoBuf_Deserialize()
    {
        using var ms = new MemoryStream(_protoBufBytes);
        return ProtoBuf.Serializer.Deserialize<SampleModel>(ms);
    }

    [Benchmark]
    public byte[] MemoryPack_Serialize() => MemoryPackSerializer.Serialize(_model);

    [Benchmark]
    public SampleModel MemoryPack_Deserialize() => MemoryPackSerializer.Deserialize<SampleModel>(_memoryPackBytes)!;
}
