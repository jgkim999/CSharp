using BenchmarkDotNet.Running;
using UlidPractice;

internal class Program
{
    /// <summary>
    /// dotnet run -project UlidPractice.csproj -c Release
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
        var ulid = new UlidBenchmark();
        ulid.Test1();
        ulid.Test2();
        var summary = BenchmarkRunner.Run<UlidBenchmark>();
    }
}