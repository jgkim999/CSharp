using BenchmarkDotNet.Running;
namespace Demo.Infra.SerializationBenchmarks {
	public class Program {
		public static void Main(string[] args) {
			BenchmarkRunner.Run<SerializationBenchmarks>();
		}
	}
}
