using Demo.Application.Configs;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

namespace Demo.Application.Extensions;

/// <summary>
/// 메트릭 처리 전략을 구현하는 클래스
/// </summary>
public static class MetricProcessingStrategies
{
    /// <summary>
    /// 환경별 메트릭 리더를 생성합니다.
    /// </summary>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <param name="environment">환경 이름</param>
    /// <returns>구성된 메트릭 리더</returns>
    public static MetricReader CreateEnvironmentBasedMetricReader(OpenTelemetryConfig config, string environment)
    {
        // OpenTelemetryExtensions의 CreateMetricExporter 메서드를 사용하기 위해
        // 리플렉션을 통해 호출하거나, 직접 구현
        var exporter = CreateMetricExporterInternal(config);
        
        return environment.ToLowerInvariant() switch
        {
            "development" => CreateDevelopmentMetricReader(exporter, config),
            "staging" => CreateStagingMetricReader(exporter, config),
            "production" => CreateProductionMetricReader(exporter, config),
            _ => CreateDefaultMetricReader(exporter, config)
        };
    }

    /// <summary>
    /// 개발 환경용 메트릭 리더를 생성합니다.
    /// </summary>
    /// <param name="exporter">메트릭 익스포터</param>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <returns>개발 환경용 메트릭 리더</returns>
    private static MetricReader CreateDevelopmentMetricReader(BaseExporter<Metric> exporter, OpenTelemetryConfig config)
    {
        return new PeriodicExportingMetricReader(
            exporter: exporter,
            exportIntervalMilliseconds: Math.Min(config.Metrics.BatchExportIntervalMilliseconds, 2000), // 최대 2초
            exportTimeoutMilliseconds: Math.Min(config.Metrics.ExportTimeoutMilliseconds, 5000))        // 최대 5초
        {
            TemporalityPreference = MetricReaderTemporalityPreference.Cumulative
        };
    }

    /// <summary>
    /// 스테이징 환경용 메트릭 리더를 생성합니다.
    /// </summary>
    /// <param name="exporter">메트릭 익스포터</param>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <returns>스테이징 환경용 메트릭 리더</returns>
    private static MetricReader CreateStagingMetricReader(BaseExporter<Metric> exporter, OpenTelemetryConfig config)
    {
        return new PeriodicExportingMetricReader(
            exporter: new BatchingMetricExporter(exporter, config),
            exportIntervalMilliseconds: config.Metrics.BatchExportIntervalMilliseconds,
            exportTimeoutMilliseconds: config.Metrics.ExportTimeoutMilliseconds)
        {
            TemporalityPreference = MetricReaderTemporalityPreference.Delta
        };
    }

    /// <summary>
    /// 프로덕션 환경용 메트릭 리더를 생성합니다.
    /// </summary>
    /// <param name="exporter">메트릭 익스포터</param>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <returns>프로덕션 환경용 메트릭 리더</returns>
    private static MetricReader CreateProductionMetricReader(BaseExporter<Metric> exporter, OpenTelemetryConfig config)
    {
        var batchingExporter = new BatchingMetricExporter(exporter, config);
        var resilientExporter = new ResilientMetricExporter(batchingExporter, config);
        
        return new PeriodicExportingMetricReader(
            exporter: resilientExporter,
            exportIntervalMilliseconds: config.Metrics.BatchExportIntervalMilliseconds,
            exportTimeoutMilliseconds: config.Metrics.ExportTimeoutMilliseconds)
        {
            TemporalityPreference = MetricReaderTemporalityPreference.Delta
        };
    }

    /// <summary>
    /// 기본 메트릭 리더를 생성합니다.
    /// </summary>
    /// <param name="exporter">메트릭 익스포터</param>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <returns>기본 메트릭 리더</returns>
    private static MetricReader CreateDefaultMetricReader(BaseExporter<Metric> exporter, OpenTelemetryConfig config)
    {
        return new PeriodicExportingMetricReader(
            exporter: exporter,
            exportIntervalMilliseconds: config.Metrics.BatchExportIntervalMilliseconds,
            exportTimeoutMilliseconds: config.Metrics.ExportTimeoutMilliseconds);
    }

    /// <summary>
    /// 메트릭 익스포터를 생성합니다.
    /// </summary>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <returns>메트릭 익스포터</returns>
    private static BaseExporter<Metric> CreateMetricExporterInternal(OpenTelemetryConfig config)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var exporterType = config.Exporter.Type.ToLowerInvariant();

        // 개발 환경에서는 항상 콘솔 익스포터 사용
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            return new ConsoleMetricExporter(new ConsoleExporterOptions
            {
                Targets = ConsoleExporterOutputTargets.Console
            });
        }

        return exporterType switch
        {
            "console" => new ConsoleMetricExporter(new ConsoleExporterOptions
            {
                Targets = ConsoleExporterOutputTargets.Console
            }),
            
            "otlp" when !string.IsNullOrEmpty(config.Exporter.OtlpEndpoint) => 
                new OtlpMetricExporter(new OtlpExporterOptions
                {
                    Endpoint = new Uri(config.Exporter.OtlpEndpoint),
                    Protocol = config.Exporter.OtlpProtocol.ToLowerInvariant() switch
                    {
                        "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                        "grpc" => OtlpExportProtocol.Grpc,
                        _ => OtlpExportProtocol.Grpc
                    },
                    TimeoutMilliseconds = config.Exporter.TimeoutMilliseconds,
                    Headers = config.Exporter.OtlpHeaders.Count > 0 
                        ? string.Join(",", config.Exporter.OtlpHeaders.Select(h => $"{h.Key}={h.Value}"))
                        : null
                }),
            
            _ => new ConsoleMetricExporter(new ConsoleExporterOptions
            {
                Targets = ConsoleExporterOutputTargets.Console
            })
        };
    }
}

/// <summary>
/// 메트릭 배치 처리를 수행하는 익스포터
/// </summary>
public class BatchingMetricExporter : BaseExporter<Metric>
{
    private readonly BaseExporter<Metric> _innerExporter;
    private readonly OpenTelemetryConfig _config;
    private readonly List<Metric> _batch;
    private readonly Timer _exportTimer;
    private readonly object _lock = new();
    private volatile bool _disposed;

    /// <summary>
    /// BatchingMetricExporter 생성자
    /// </summary>
    /// <param name="innerExporter">내부 익스포터</param>
    /// <param name="config">OpenTelemetry 구성</param>
    public BatchingMetricExporter(BaseExporter<Metric> innerExporter, OpenTelemetryConfig config)
    {
        _innerExporter = innerExporter ?? throw new ArgumentNullException(nameof(innerExporter));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _batch = new List<Metric>(config.Metrics.MaxBatchSize);
        
        // 주기적으로 배치 익스포트 수행
        _exportTimer = new Timer(
            ExportBatch, 
            null, 
            TimeSpan.FromMilliseconds(config.Metrics.BatchExportIntervalMilliseconds),
            TimeSpan.FromMilliseconds(config.Metrics.BatchExportIntervalMilliseconds));
    }

    /// <summary>
    /// 메트릭을 익스포트합니다.
    /// </summary>
    /// <param name="batch">메트릭 배치</param>
    /// <returns>익스포트 결과</returns>
    public override ExportResult Export(in Batch<Metric> batch)
    {
        if (_disposed)
        {
            return ExportResult.Failure;
        }

        lock (_lock)
        {
            foreach (var metric in batch)
            {
                _batch.Add(metric);
                
                // 배치 크기가 최대치에 도달하면 즉시 익스포트
                if (_batch.Count >= _config.Metrics.MaxBatchSize)
                {
                    var result = ExportBatchInternal();
                    if (result != ExportResult.Success)
                    {
                        return result;
                    }
                }
            }
        }

        return ExportResult.Success;
    }

    /// <summary>
    /// 주기적으로 배치를 익스포트합니다.
    /// </summary>
    /// <param name="state">타이머 상태</param>
    private void ExportBatch(object? state)
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_batch.Count > 0)
            {
                ExportBatchInternal();
            }
        }
    }

    /// <summary>
    /// 내부적으로 배치를 익스포트합니다.
    /// </summary>
    /// <returns>익스포트 결과</returns>
    private ExportResult ExportBatchInternal()
    {
        if (_batch.Count == 0)
        {
            return ExportResult.Success;
        }

        try
        {
            var batchToExport = _batch.ToArray();
            _batch.Clear();

            var batch = new Batch<Metric>(batchToExport, batchToExport.Length);
            return _innerExporter.Export(batch);
        }
        catch (Exception ex)
        {
            // 로깅 (실제 구현에서는 ILogger 사용)
            Console.WriteLine($"메트릭 배치 익스포트 중 오류 발생: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    /// <summary>
    /// 리소스를 해제합니다.
    /// </summary>
    /// <param name="disposing">해제 여부</param>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;
            _exportTimer?.Dispose();
            
            // 남은 배치 익스포트
            lock (_lock)
            {
                if (_batch.Count > 0)
                {
                    ExportBatchInternal();
                }
            }
            
            _innerExporter?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}

/// <summary>
/// 재시도 정책과 회복력을 제공하는 메트릭 익스포터
/// </summary>
public class ResilientMetricExporter : BaseExporter<Metric>
{
    private readonly BaseExporter<Metric> _innerExporter;
    private readonly RetryPolicyConfig _retryConfig;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// ResilientMetricExporter 생성자
    /// </summary>
    /// <param name="innerExporter">내부 익스포터</param>
    /// <param name="config">OpenTelemetry 구성</param>
    public ResilientMetricExporter(BaseExporter<Metric> innerExporter, OpenTelemetryConfig config)
    {
        _innerExporter = innerExporter ?? throw new ArgumentNullException(nameof(innerExporter));
        _retryConfig = config.Exporter.RetryPolicy;
        
        // 동시 익스포트 제한 (메모리 사용량 제어)
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
    }

    /// <summary>
    /// 재시도 정책을 적용하여 메트릭을 익스포트합니다.
    /// </summary>
    /// <param name="batch">메트릭 배치</param>
    /// <returns>익스포트 결과</returns>
    public override ExportResult Export(in Batch<Metric> batch)
    {
        if (!_retryConfig.Enabled)
        {
            return _innerExporter.Export(batch);
        }

        return ExportWithRetry(batch);
    }

    /// <summary>
    /// 재시도 로직을 적용하여 익스포트를 수행합니다.
    /// </summary>
    /// <param name="batch">메트릭 배치</param>
    /// <returns>익스포트 결과</returns>
    private ExportResult ExportWithRetry(in Batch<Metric> batch)
    {
        var attempt = 0;
        var backoffMs = _retryConfig.InitialBackoffMs;

        while (attempt <= _retryConfig.MaxRetryAttempts)
        {
            try
            {
                // 동시 익스포트 제한
                if (!_semaphore.Wait(TimeSpan.FromMilliseconds(1000)))
                {
                    return ExportResult.Failure;
                }

                try
                {
                    var result = _innerExporter.Export(batch);
                    
                    if (result == ExportResult.Success)
                    {
                        return result;
                    }

                    // 실패한 경우 재시도 여부 결정
                    if (attempt >= _retryConfig.MaxRetryAttempts)
                    {
                        return result;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _semaphore.Release();
                
                // 마지막 시도인 경우 예외 전파
                if (attempt >= _retryConfig.MaxRetryAttempts)
                {
                    Console.WriteLine($"메트릭 익스포트 최종 실패: {ex.Message}");
                    return ExportResult.Failure;
                }
            }

            // 백오프 대기
            if (attempt < _retryConfig.MaxRetryAttempts)
            {
                Thread.Sleep(backoffMs);
                backoffMs = Math.Min(
                    (int)(backoffMs * _retryConfig.BackoffMultiplier), 
                    _retryConfig.MaxBackoffMs);
            }

            attempt++;
        }

        return ExportResult.Failure;
    }

    /// <summary>
    /// 리소스를 해제합니다.
    /// </summary>
    /// <param name="disposing">해제 여부</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore?.Dispose();
            _innerExporter?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}

/// <summary>
/// 메모리 사용량을 모니터링하고 제한하는 메트릭 익스포터
/// </summary>
public class MemoryLimitedMetricExporter : BaseExporter<Metric>
{
    private readonly BaseExporter<Metric> _innerExporter;
    private readonly long _maxMemoryUsageBytes;
    private readonly Timer _memoryCheckTimer;
    private volatile bool _memoryLimitExceeded;

    /// <summary>
    /// MemoryLimitedMetricExporter 생성자
    /// </summary>
    /// <param name="innerExporter">내부 익스포터</param>
    /// <param name="maxMemoryUsageMB">최대 메모리 사용량 (MB)</param>
    public MemoryLimitedMetricExporter(BaseExporter<Metric> innerExporter, int maxMemoryUsageMB)
    {
        _innerExporter = innerExporter ?? throw new ArgumentNullException(nameof(innerExporter));
        _maxMemoryUsageBytes = maxMemoryUsageMB * 1024L * 1024L;
        
        // 30초마다 메모리 사용량 확인
        _memoryCheckTimer = new Timer(CheckMemoryUsage, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 메모리 제한을 확인하고 메트릭을 익스포트합니다.
    /// </summary>
    /// <param name="batch">메트릭 배치</param>
    /// <returns>익스포트 결과</returns>
    public override ExportResult Export(in Batch<Metric> batch)
    {
        // 메모리 제한 초과 시 익스포트 중단
        if (_memoryLimitExceeded)
        {
            return ExportResult.Failure;
        }

        return _innerExporter.Export(batch);
    }

    /// <summary>
    /// 메모리 사용량을 확인합니다.
    /// </summary>
    /// <param name="state">타이머 상태</param>
    private void CheckMemoryUsage(object? state)
    {
        try
        {
            var currentMemory = GC.GetTotalMemory(false);
            _memoryLimitExceeded = currentMemory > _maxMemoryUsageBytes;

            if (_memoryLimitExceeded)
            {
                Console.WriteLine($"메모리 사용량 제한 초과: {currentMemory / 1024 / 1024}MB > {_maxMemoryUsageBytes / 1024 / 1024}MB");
                
                // 강제 가비지 컬렉션 수행
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // 가비지 컬렉션 후 다시 확인
                currentMemory = GC.GetTotalMemory(false);
                _memoryLimitExceeded = currentMemory > _maxMemoryUsageBytes;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"메모리 사용량 확인 중 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 리소스를 해제합니다.
    /// </summary>
    /// <param name="disposing">해제 여부</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _memoryCheckTimer?.Dispose();
            _innerExporter?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}