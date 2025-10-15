```

BenchmarkDotNet v0.15.4, macOS 26.0.1 (25A362) [Darwin 25.0.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Method: 벤치마크로 측정한 함수(메서드) 이름입니다.

Mean: 평균 실행 시간(ns, 나노초)입니다. 여러 번 반복 측정한 값의 평균입니다.

Error: 신뢰 구간(99.9%)의 절반 값으로, 측정값의 오차 범위를 나타냅니다.

StdDev: 표준편차로, 측정값의 분산(흩어짐) 정도를 나타냅니다.

Median: 중앙값(50% 지점)으로, 전체 측정값 중 중간에 위치한 값입니다.

Gen0: GC(가비지 컬렉션) 0세대가 1000번 실행당 몇 번 발생했는지 나타냅니다. 메모리 관리 효율을 볼 수 있습니다.

Allocated: 한 번 실행할 때 할당된 메모리 크기(바이트)입니다.

```
| Method              | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| StringFormat        | 43.207 ns | 0.7344 ns | 0.9549 ns |  1.00 |    0.03 | 0.0067 |      56 B |        1.00 |
| StringIterpolation  | 48.092 ns | 0.3353 ns | 0.2972 ns |  1.11 |    0.02 | 0.0124 |     104 B |        1.86 |
| NaiveLogDebug       | 43.593 ns | 0.1595 ns | 0.1492 ns |  1.01 |    0.02 | 0.0067 |      56 B |        1.00 |
| IsEnabledGuard      |  2.337 ns | 0.0157 ns | 0.0123 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| LoggerMessageDefine |  2.841 ns | 0.0063 ns | 0.0049 ns |  0.07 |    0.00 |      - |         - |        0.00 |
| SourceGenerated     |  2.326 ns | 0.0017 ns | 0.0015 ns |  0.05 |    0.00 |      - |         - |        0.00 |
