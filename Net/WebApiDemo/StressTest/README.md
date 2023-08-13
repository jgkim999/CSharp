# 목차

- [목차](#목차)
  - [문서화](#문서화)

## [K6](https://k6.io/)

[Install](https://k6.io/docs/get-started/installation/)

[Windows Download](https://dl.k6.io/msi/k6-latest-amd64.msi)

## Run

Test Run

```bash
k6 run stressTest.js
```

Test Result

```bash

          /\      |‾‾| /‾‾/   /‾‾/
     /\  /  \     |  |/  /   /  /
    /  \/    \    |     (   /   ‾‾\
   /          \   |  |\  \ |  (‾)  |
  / __________ \  |__| \__\ \_____/ .io

  execution: local
     script: stressTest.js
     output: -

  scenarios: (100.00%) 1 scenario, 100 max VUs, 2m30s max duration (incl. graceful stop):
           * default: Up to 100 looping VUs for 2m0s over 3 stages (gracefulRampDown: 30s, gracefulStop: 30s)

     ✓ login succeeded

     checks.........................: 100.00% ✓ 237409      ✗ 0
     data_received..................: 66 MB   547 kB/s
     data_sent......................: 63 MB   524 kB/s
     http_req_blocked...............: avg=2.99µs  min=0s     med=0s      max=9.82ms   p(90)=0s       p(95)=0s
     http_req_connecting............: avg=292ns   min=0s     med=0s      max=3.37ms   p(90)=0s       p(95)=0s
     http_req_duration..............: avg=18.91ms min=0s     med=6.73ms  max=250.85ms p(90)=47.3ms   p(95)=53.04ms
       { expected_response:true }...: avg=18.91ms min=0s     med=6.73ms  max=250.85ms p(90)=47.3ms   p(95)=53.04ms
     http_req_failed................: 0.00%   ✓ 0           ✗ 474818
     http_req_receiving.............: avg=58.34µs min=0s     med=0s      max=23.61ms  p(90)=138.19µs p(95)=518.9µs
     http_req_sending...............: avg=59.09µs min=0s     med=0s      max=7.46ms   p(90)=436.68µs p(95)=522.4µs
     http_req_tls_handshaking.......: avg=706ns   min=0s     med=0s      max=8.44ms   p(90)=0s       p(95)=0s
     http_req_waiting...............: avg=18.79ms min=0s     med=6.4ms   max=250.85ms p(90)=47.11ms  p(95)=52.84ms
     http_reqs......................: 474818  3956.604392/s
     iteration_duration.............: avg=37.96ms min=5.42ms med=38.62ms max=250.85ms p(90)=53.85ms  p(95)=59.65ms
     iterations.....................: 237409  1978.302196/s
     vus............................: 1       min=1         max=100
     vus_max........................: 100     min=100       max=100

running (2m00.0s), 000/100 VUs, 237409 complete and 0 interrupted iterations
default ✓ [======================================] 000/100 VUs  2m0s
```

## [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)

Install

```bash
dotnet tool install --global dotnet-counters
```

Get pid

```bash
dotnet-counters ps
```

Monitor pid

```bash
dotnet-counters monitor -p [pid]
```

Monitor Result

```bash
Press p to pause, r to resume, q to quit.
    Status: Running

[System.Runtime]
    % Time in GC since last GC (%)                                         0
    Allocation Rate (B / 1 sec)                                   70,695,040
    CPU Usage (%)                                                          1.429
    Exception Count (Count / 1 sec)                                        0
    GC Committed Bytes (MB)                                              182.256
    GC Fragmentation (%)                                                  79.573
    GC Heap Size (MB)                                                     -0.082
    Gen 0 GC Count (Count / 1 sec)                                         1
    Gen 0 Size (B)                                                73,975,632
    Gen 1 GC Count (Count / 1 sec)                                         1
    Gen 1 Size (B)                                                 9,099,968
    Gen 2 GC Count (Count / 1 sec)                                         0
    Gen 2 Size (B)                                                21,321,288
    IL Bytes Jitted (B)                                            1,476,764
    LOH Size (B)                                                   2,752,352
    Monitor Lock Contention Count (Count / 1 sec)                        151
    Number of Active Timers                                               16
    Number of Assemblies Loaded                                          181
    Number of Methods Jitted                                          18,888
    POH (Pinned Object Heap) Size (B)                              1,484,464
    ThreadPool Completed Work Item Count (Count / 1 sec)              43,918
    ThreadPool Queue Length                                                0
    ThreadPool Thread Count                                               15
    Time spent in JIT (ms / 1 sec)                                         0
    Working Set (MB)                                                     300.351
```
