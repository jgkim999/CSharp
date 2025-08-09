using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime;

namespace Demo.Web.PerformanceTests.Benchmarks;

/// <summary>
/// 메모리 사용량 성능 벤치마크
/// OpenTelemetry 도입으로 인한 메모리 사용량 증가를 측정합니다
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 50)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class MemoryUsageBenchmark
{
    private WebApplicationFactory<Program>? _factoryWithoutOtel;
    private WebApplicationFactory<Program>? _factoryWithOtel;
    private HttpClient? _clientWithoutOtel;
    private HttpClient? _clientWithOtel;

    /// <summary>
    /// 벤치마크 실행 전 초기화를 수행합니다
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // 가비지 컬렉션 강제 실행
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // OpenTelemetry 없는 팩토리 설정
        _factoryWithoutOtel = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // OpenTelemetry 서비스 제거
                    var openTelemetryDescriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("OpenTelemetry") == true)
                        .ToList();
                    
                    foreach (var descriptor in openTelemetryDescriptors)
                    {
                        services.Remove(descriptor);
                    }
                });
                
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Error);
                });
            });

        // OpenTelemetry 있는 팩토리 설정
        _factoryWithOtel = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Error);
                });
            });

        _clientWithoutOtel = _factoryWithoutOtel.CreateClient();
        _clientWithOtel = _factoryWithOtel.CreateClient();
    }

    /// <summary>
    /// 벤치마크 실행 후 정리를 수행합니다
    /// </summary>
    [GlobalCleanup]
    public async Task Cleanup()
    {
        _clientWithoutOtel?.Dispose();
        _clientWithOtel?.Dispose();
        
        if (_factoryWithoutOtel != null)
            await _factoryWithoutOtel.DisposeAsync();
        
        if (_factoryWithOtel != null)
            await _factoryWithOtel.DisposeAsync();
    }

    /// <summary>
    /// OpenTelemetry 없이 메모리 사용량을 측정합니다
    /// </summary>
    /// <returns>메모리 사용량 정보</returns>
    [Benchmark(Baseline = true)]
    public async Task<MemoryInfo> MeasureMemoryWithoutOtel()
    {
        var initialMemory = GC.GetTotalMemory(false);
        
        // 여러 요청을 통해 메모리 사용량 측정
        for (int i = 0; i < 100; i++)
        {
            var response = await _clientWithoutOtel!.GetAsync("/health");
            response.Dispose();
        }

        // 가비지 컬렉션 후 메모리 측정
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            InitialMemory = initialMemory,
            FinalMemory = finalMemory,
            MemoryDifference = finalMemory - initialMemory,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    /// <summary>
    /// OpenTelemetry와 함께 메모리 사용량을 측정합니다
    /// </summary>
    /// <returns>메모리 사용량 정보</returns>
    [Benchmark]
    public async Task<MemoryInfo> MeasureMemoryWithOtel()
    {
        var initialMemory = GC.GetTotalMemory(false);
        
        // 여러 요청을 통해 메모리 사용량 측정
        for (int i = 0; i < 100; i++)
        {
            var response = await _clientWithOtel!.GetAsync("/health");
            response.Dispose();
        }

        // 가비지 컬렉션 후 메모리 측정
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            InitialMemory = initialMemory,
            FinalMemory = finalMemory,
            MemoryDifference = finalMemory - initialMemory,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    /// <summary>
    /// 지속적인 요청 처리 중 메모리 누수를 확인합니다 (OpenTelemetry 없음)
    /// </summary>
    /// <returns>메모리 사용량 정보</returns>
    [Benchmark]
    public async Task<MemoryInfo> MemoryLeakTestWithoutOtel()
    {
        var initialMemory = GC.GetTotalMemory(false);
        
        // 대량의 요청을 처리하여 메모리 누수 확인
        for (int i = 0; i < 1000; i++)
        {
            var response = await _clientWithoutOtel!.GetAsync("/health");
            response.Dispose();
            
            // 주기적으로 가비지 컬렉션 실행
            if (i % 100 == 0)
            {
                GC.Collect();
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            InitialMemory = initialMemory,
            FinalMemory = finalMemory,
            MemoryDifference = finalMemory - initialMemory,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    /// <summary>
    /// 지속적인 요청 처리 중 메모리 누수를 확인합니다 (OpenTelemetry 포함)
    /// </summary>
    /// <returns>메모리 사용량 정보</returns>
    [Benchmark]
    public async Task<MemoryInfo> MemoryLeakTestWithOtel()
    {
        var initialMemory = GC.GetTotalMemory(false);
        
        // 대량의 요청을 처리하여 메모리 누수 확인
        for (int i = 0; i < 1000; i++)
        {
            var response = await _clientWithOtel!.GetAsync("/health");
            response.Dispose();
            
            // 주기적으로 가비지 컬렉션 실행
            if (i % 100 == 0)
            {
                GC.Collect();
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            InitialMemory = initialMemory,
            FinalMemory = finalMemory,
            MemoryDifference = finalMemory - initialMemory,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    /// <summary>
    /// 프로세스 메모리 사용량을 측정합니다 (OpenTelemetry 없음)
    /// </summary>
    /// <returns>프로세스 메모리 정보</returns>
    [Benchmark]
    public async Task<ProcessMemoryInfo> MeasureProcessMemoryWithoutOtel()
    {
        var process = Process.GetCurrentProcess();
        var initialWorkingSet = process.WorkingSet64;
        var initialPrivateMemory = process.PrivateMemorySize64;
        
        // 요청 처리
        for (int i = 0; i < 500; i++)
        {
            var response = await _clientWithoutOtel!.GetAsync("/health");
            response.Dispose();
        }

        process.Refresh();
        var finalWorkingSet = process.WorkingSet64;
        var finalPrivateMemory = process.PrivateMemorySize64;
        
        return new ProcessMemoryInfo
        {
            InitialWorkingSet = initialWorkingSet,
            FinalWorkingSet = finalWorkingSet,
            WorkingSetDifference = finalWorkingSet - initialWorkingSet,
            InitialPrivateMemory = initialPrivateMemory,
            FinalPrivateMemory = finalPrivateMemory,
            PrivateMemoryDifference = finalPrivateMemory - initialPrivateMemory
        };
    }

    /// <summary>
    /// 프로세스 메모리 사용량을 측정합니다 (OpenTelemetry 포함)
    /// </summary>
    /// <returns>프로세스 메모리 정보</returns>
    [Benchmark]
    public async Task<ProcessMemoryInfo> MeasureProcessMemoryWithOtel()
    {
        var process = Process.GetCurrentProcess();
        var initialWorkingSet = process.WorkingSet64;
        var initialPrivateMemory = process.PrivateMemorySize64;
        
        // 요청 처리
        for (int i = 0; i < 500; i++)
        {
            var response = await _clientWithOtel!.GetAsync("/health");
            response.Dispose();
        }

        process.Refresh();
        var finalWorkingSet = process.WorkingSet64;
        var finalPrivateMemory = process.PrivateMemorySize64;
        
        return new ProcessMemoryInfo
        {
            InitialWorkingSet = initialWorkingSet,
            FinalWorkingSet = finalWorkingSet,
            WorkingSetDifference = finalWorkingSet - initialWorkingSet,
            InitialPrivateMemory = initialPrivateMemory,
            FinalPrivateMemory = finalPrivateMemory,
            PrivateMemoryDifference = finalPrivateMemory - initialPrivateMemory
        };
    }
}

/// <summary>
/// 메모리 사용량 정보를 담는 구조체
/// </summary>
public struct MemoryInfo
{
    public long InitialMemory { get; set; }
    public long FinalMemory { get; set; }
    public long MemoryDifference { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}

/// <summary>
/// 프로세스 메모리 정보를 담는 구조체
/// </summary>
public struct ProcessMemoryInfo
{
    public long InitialWorkingSet { get; set; }
    public long FinalWorkingSet { get; set; }
    public long WorkingSetDifference { get; set; }
    public long InitialPrivateMemory { get; set; }
    public long FinalPrivateMemory { get; set; }
    public long PrivateMemoryDifference { get; set; }
}