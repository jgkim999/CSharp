```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.6 (24G84) [Darwin 24.6.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), Arm64 RyuJIT AdvSIMD
  Job-BKYGZI : .NET 9.0.7 (9.0.725.31616), Arm64 RyuJIT AdvSIMD

IterationCount=10  RunStrategy=ColdStart  

```
| Method                       | Mean | Error | Min | Max | Median | Ratio | RatioSD | Alloc Ratio |
|----------------------------- |-----:|------:|----:|----:|-------:|------:|--------:|------------:|
| StartupWithoutOpenTelemetry  |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| StartupWithOpenTelemetry     |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| StartupWithDevelopmentConfig |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |
| StartupWithProductionConfig  |   NA |    NA |  NA |  NA |     NA |     ? |       ? |           ? |

Benchmarks with issues:
  ApplicationStartupBenchmark.StartupWithoutOpenTelemetry: Job-BKYGZI(IterationCount=10, RunStrategy=ColdStart)
  ApplicationStartupBenchmark.StartupWithOpenTelemetry: Job-BKYGZI(IterationCount=10, RunStrategy=ColdStart)
  ApplicationStartupBenchmark.StartupWithDevelopmentConfig: Job-BKYGZI(IterationCount=10, RunStrategy=ColdStart)
  ApplicationStartupBenchmark.StartupWithProductionConfig: Job-BKYGZI(IterationCount=10, RunStrategy=ColdStart)
