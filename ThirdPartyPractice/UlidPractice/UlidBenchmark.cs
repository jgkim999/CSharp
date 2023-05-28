using BenchmarkDotNet.Attributes;
using FluentAssertions;

namespace UlidPractice
{
    [MemoryDiagnoser]
    public class UlidBenchmark
    {
        [GlobalSetup]
        public void GlobalSetup()
        {
            //Write your initialization code here
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            //Write your cleanup logic here
        }

        [Benchmark]
        public void NewUlid()
        {
            Ulid ulid = Ulid.NewUlid();
        }

        [Benchmark]
        public void NewGuid()
        {
            Guid guid = Guid.NewGuid();
        }

        public void Test1()
        {
            Ulid ulid = Ulid.NewUlid();
            Console.WriteLine(ulid.ToString());
            Ulid newUlid;
            if (Ulid.TryParse(ulid.ToString(), out newUlid) == true)
            {
                ulid.Equals(newUlid).Should().BeTrue();
            }
        }

        public void Test2()
        {
            Ulid ulid = Ulid.NewUlid();
            Guid guid = ulid.ToGuid();

            Ulid ulid2 = new(guid);

            ulid.Equals(ulid2).Should().BeTrue();
        }
    }
}
