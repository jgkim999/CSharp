using BenchmarkDotNet.Attributes;

namespace Demo.Zen;

[MarkdownExporter] 
[MemoryDiagnoser]
public class SpanMemoryBenchmarks
{
    private readonly string _data = "timestamp,INFO,User login successful for user123";
    [GlobalSetup]
    public void Setup()
    {
    }
    
    [Benchmark(Baseline = true)]
    public string WithoutSpan()
    {
        var parts = _data.Split(',');
        var message = parts[2];
        var userPart = message.Substring(message.IndexOf("user", StringComparison.Ordinal) + 4);
        return userPart.Trim();
    }
    
    [Benchmark]
    public ReadOnlySpan<char> WithSpan()
    {
        var span = _data.AsSpan();
        var lastComma = span.LastIndexOf(',');
        var messagePart = span.Slice(lastComma + 1);
        var userIndex = messagePart.IndexOf("user".AsSpan()) + 4;
        return messagePart.Slice(userIndex).Trim();
    }
}
