```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.6 (24G84) [Darwin 24.6.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), Arm64 RyuJIT AdvSIMD
  Job-QDDUVB : .NET 9.0.7 (9.0.725.31616), Arm64 RyuJIT AdvSIMD

IterationCount=100  

```
| Method                         | Mean | Error | Min | Max | Median | Ratio | RatioSD | Alloc Ratio |
|------------------------------- |-----:|------:|----:|----:|-------:|------:|--------:|------------:|
| SimpleGetRequestWithoutOtel    |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| SimpleGetRequestWithOtel       |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| UserCreateRequestWithoutOtel   |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| UserCreateRequestWithOtel      |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| MultipleRequestsWithoutOtel    |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| MultipleRequestsWithOtel       |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| LargePayloadRequestWithoutOtel |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| LargePayloadRequestWithOtel    |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |

Benchmarks with issues:
  HttpRequestBenchmark.SimpleGetRequestWithoutOtel: Job-QDDUVB(IterationCount=100)
  HttpRequestBenchmark.SimpleGetRequestWithOtel: Job-QDDUVB(IterationCount=100)
  HttpRequestBenchmark.UserCreateRequestWithoutOtel: Job-QDDUVB(IterationCount=100)
  HttpRequestBenchmark.UserCreateRequestWithOtel: Job-QDDUVB(IterationCount=100)
  HttpRequestBenchmark.MultipleRequestsWithoutOtel: Job-QDDUVB(IterationCount=100)
  HttpRequestBenchmark.MultipleRequestsWithOtel: Job-QDDUVB(IterationCount=100)
  HttpRequestBenchmark.LargePayloadRequestWithoutOtel: Job-QDDUVB(IterationCount=100)
  HttpRequestBenchmark.LargePayloadRequestWithOtel: Job-QDDUVB(IterationCount=100)
