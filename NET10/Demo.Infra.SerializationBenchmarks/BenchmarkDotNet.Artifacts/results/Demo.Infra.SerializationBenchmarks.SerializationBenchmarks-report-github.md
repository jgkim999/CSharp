```

BenchmarkDotNet v0.15.4, macOS 26.0.1 (25A362) [Darwin 25.0.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 9.0.7 (9.0.7, 9.0.725.31616), Arm64 RyuJIT armv8.0-a


```

Method: 벤치마크로 측정한 함수(메서드) 이름입니다.
Mean: 평균 실행 시간(ns, 나노초)입니다. 여러 번 반복 측정한 값의 평균입니다.
Error: 신뢰 구간(99.9%)의 절반 값으로, 측정값의 오차 범위를 나타냅니다.
StdDev: 표준편차로, 측정값의 분산(흩어짐) 정도를 나타냅니다.
Median: 중앙값(50% 지점)으로, 전체 측정값 중 중간에 위치한 값입니다.
Gen0: GC(가비지 컬렉션) 0세대가 1000번 실행당 몇 번 발생했는지 나타냅니다. 메모리 관리 효율을 볼 수 있습니다.
Allocated: 한 번 실행할 때 할당된 메모리 크기(바이트)입니다.

| Method                  | Mean      | Error    | StdDev   | Median    | Gen0   | Allocated |
|------------------------ |----------:|---------:|---------:|----------:|-------:|----------:|
| MemoryPack_Serialize    |  52.15 ns | 0.912 ns | 0.853 ns |  52.22 ns | 0.0067 |      56 B |
| MessagePack_Serialize   |  60.99 ns | 0.215 ns | 0.180 ns |  61.00 ns | 0.0057 |      48 B |
| ProtoBuf_Serialize      | 164.70 ns | 3.118 ns | 5.209 ns | 162.31 ns | 0.0467 |     392 B |
| MemoryPack_Deserialize  |  47.14 ns | 0.841 ns | 0.787 ns |  47.01 ns | 0.0086 |      72 B |
| MessagePack_Deserialize |  85.31 ns | 1.310 ns | 1.161 ns |  85.08 ns | 0.0086 |      72 B |
| ProtoBuf_Deserialize    | 191.40 ns | 2.918 ns | 2.436 ns | 192.07 ns | 0.0191 |     160 B |
