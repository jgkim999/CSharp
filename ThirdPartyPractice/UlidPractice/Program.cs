using BenchmarkDotNet.Running;
using UlidPractice;

internal class Program
{
    private static void Main(string[] args)
    {
        var ulid = new UlidBenchmark();
        ulid.Test1();
        ulid.Test2();
        // dotnet run -project UlidPractice.csproj -c Release
        var summary = BenchmarkRunner.Run<UlidBenchmark>();
    }
}