using System.Diagnostics;
using Demo.Application.Services;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Decorators;

/// <summary>
/// 명령 핸들러에 텔레메트리 추적을 추가하는 데코레이터 클래스
/// </summary>
/// <typeparam name="TCommand">명령 타입</typeparam>
/// <typeparam name="TResult">결과 타입</typeparam>
public class TelemetryCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : class, ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _innerHandler;
    private readonly ILogger<TelemetryCommandHandlerDecorator<TCommand, TResult>> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// TelemetryCommandHandlerDecorator 생성자
    /// Initializes a new instance of the <see cref="TelemetryCommandHandlerDecorator{TCommand, TResult}"/> class, enabling telemetry tracing and logging for command handling operations.
    /// </summary>
    /// <param name="innerHandler">실제 명령 핸들러</param>
    /// <param name="logger">로거 인스턴스</param>
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryCommandHandlerDecorator{TCommand, TResult}"/> class, wrapping a command handler with telemetry tracing and logging capabilities.
    /// </summary>
    /// <param name="innerHandler">The command handler to be decorated.</param>
    /// <param name="logger">The logger used for structured logging of command execution.</param>
    /// <param name="telemetryService">The telemetry service used to record tracing and metrics for command processing.</param>
    public TelemetryCommandHandlerDecorator(
        ICommandHandler<TCommand, TResult> innerHandler,
        ILogger<TelemetryCommandHandlerDecorator<TCommand, TResult>> logger,
        ITelemetryService telemetryService)
    {
        _innerHandler = innerHandler;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// 텔레메트리 추적과 함께 명령을 처리합니다.
    /// </summary>
    /// <param name="command">처리할 명령</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <summary>
    /// Processes a LiteBus command with telemetry tracing, logging, and metrics, and returns the result.
    /// </summary>
    /// <param name="command">The command to be processed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The result produced by processing the command.</returns>
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandType = typeof(TCommand);
        var operationName = $"LiteBus.Command.{commandType.Name}";
        
        // Activity 태그 준비
        var tags = new Dictionary<string, object?>
        {
            ["command.type"] = commandType.Name,
            ["command.full_type"] = commandType.FullName,
            ["command.assembly"] = commandType.Assembly.GetName().Name,
            ["command.namespace"] = commandType.Namespace,
            ["operation.type"] = "Command",
            ["command.id"] = command.GetHashCode().ToString(),
            ["result.type"] = typeof(TResult).Name
        };

        using var activity = _telemetryService.StartActivity(operationName, tags);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 명령 시작 로그
            _telemetryService.LogInformationWithTrace(_logger, 
                "LiteBus 명령 처리 시작: {CommandType} in {Assembly}", 
                commandType.Name, commandType.Assembly.GetName().Name ?? "Unknown");

            // 실제 핸들러 실행
            var result = await _innerHandler.HandleAsync(command, cancellationToken);
            
            stopwatch.Stop();
            
            // 성공 메트릭 기록
            _telemetryService.RecordBusinessMetric("litebus_commands_total", 1, new Dictionary<string, object?>
            {
                ["command_type"] = commandType.Name,
                ["status"] = "success",
                ["assembly"] = commandType.Assembly.GetName().Name ?? "Unknown"
            });

            // 처리 시간 메트릭 기록
            _telemetryService.RecordBusinessMetric("litebus_command_duration_ms", stopwatch.ElapsedMilliseconds, new Dictionary<string, object?>
            {
                ["command_type"] = commandType.Name,
                ["assembly"] = commandType.Assembly.GetName().Name ?? "Unknown"
            });

            // Activity에 성공 상태 설정
            _telemetryService.SetActivitySuccess(activity, "LiteBus 명령 처리 완료");
            
            // 성공 로그
            _telemetryService.LogInformationWithTrace(_logger, 
                "LiteBus 명령 처리 완료: {CommandType}, 처리시간: {ElapsedMs}ms", 
                commandType.Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 에러 메트릭 기록
            _telemetryService.RecordError(
                errorType: ex.GetType().Name,
                operation: operationName,
                message: ex.Message);

            _telemetryService.RecordBusinessMetric("litebus_commands_total", 1, new Dictionary<string, object?>
            {
                ["command_type"] = commandType.Name,
                ["status"] = "error",
                ["error_type"] = ex.GetType().Name,
                ["assembly"] = commandType.Assembly.GetName().Name ?? "Unknown"
            });

            // Activity에 에러 상태 설정
            _telemetryService.SetActivityError(activity, ex);
            
            // 에러 로그
            _telemetryService.LogErrorWithTrace(_logger, ex, 
                "LiteBus 명령 처리 중 오류 발생: {CommandType}, 처리시간: {ElapsedMs}ms, 오류: {ErrorMessage}", 
                commandType.Name, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}

/// <summary>
/// void 명령 핸들러에 텔레메트리 추적을 추가하는 데코레이터 클래스
/// </summary>
/// <typeparam name="TCommand">명령 타입</typeparam>
public class TelemetryCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
    where TCommand : class, ICommand
{
    private readonly ICommandHandler<TCommand> _innerHandler;
    private readonly ILogger<TelemetryCommandHandlerDecorator<TCommand>> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// TelemetryCommandHandlerDecorator 생성자
    /// </summary>
    /// <param name="innerHandler">실제 명령 핸들러</param>
    /// <param name="logger">로거 인스턴스</param>
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryCommandHandlerDecorator{TCommand}"/> class, enabling telemetry tracing and logging for command handling.
    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryCommandHandlerDecorator{TCommand}"/> class, enabling telemetry tracing and logging for command handling.
    /// </summary>
    /// <param name="innerHandler">The command handler to be decorated with telemetry and logging.</param>
    public TelemetryCommandHandlerDecorator(
        ICommandHandler<TCommand> innerHandler,
        ILogger<TelemetryCommandHandlerDecorator<TCommand>> logger,
        ITelemetryService telemetryService)
    {
        _innerHandler = innerHandler;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// 텔레메트리 추적과 함께 명령을 처리합니다.
    /// </summary>
    /// <param name="command">처리할 명령</param>
    /// <summary>
    /// Handles a LiteBus command with integrated telemetry tracing and logging.
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// Records telemetry data, logs command execution, and tracks success or error metrics for the command handling process. Exceptions encountered during command processing are logged, recorded in telemetry, and rethrown.
    /// </remarks>
    public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandType = typeof(TCommand);
        var operationName = $"LiteBus.Command.{commandType.Name}";
        
        // Activity 태그 준비
        var tags = new Dictionary<string, object?>
        {
            ["command.type"] = commandType.Name,
            ["command.full_type"] = commandType.FullName,
            ["command.assembly"] = commandType.Assembly.GetName().Name,
            ["command.namespace"] = commandType.Namespace,
            ["operation.type"] = "Command",
            ["command.id"] = command.GetHashCode().ToString()
        };

        using var activity = _telemetryService.StartActivity(operationName, tags);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 명령 시작 로그
            _telemetryService.LogInformationWithTrace(_logger, 
                "LiteBus 명령 처리 시작: {CommandType} in {Assembly}", 
                commandType.Name, commandType.Assembly.GetName().Name ?? "Unknown");

            // 실제 핸들러 실행
            await _innerHandler.HandleAsync(command, cancellationToken);
            
            stopwatch.Stop();
            
            // 성공 메트릭 기록
            _telemetryService.RecordBusinessMetric("litebus_commands_total", 1, new Dictionary<string, object?>
            {
                ["command_type"] = commandType.Name,
                ["status"] = "success",
                ["assembly"] = commandType.Assembly.GetName().Name ?? "Unknown"
            });

            // 처리 시간 메트릭 기록
            _telemetryService.RecordBusinessMetric("litebus_command_duration_ms", stopwatch.ElapsedMilliseconds, new Dictionary<string, object?>
            {
                ["command_type"] = commandType.Name,
                ["assembly"] = commandType.Assembly.GetName().Name ?? "Unknown"
            });

            // Activity에 성공 상태 설정
            _telemetryService.SetActivitySuccess(activity, "LiteBus 명령 처리 완료");
            
            // 성공 로그
            _telemetryService.LogInformationWithTrace(_logger, 
                "LiteBus 명령 처리 완료: {CommandType}, 처리시간: {ElapsedMs}ms", 
                commandType.Name, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 에러 메트릭 기록
            _telemetryService.RecordError(
                errorType: ex.GetType().Name,
                operation: operationName,
                message: ex.Message);

            _telemetryService.RecordBusinessMetric("litebus_commands_total", 1, new Dictionary<string, object?>
            {
                ["command_type"] = commandType.Name,
                ["status"] = "error",
                ["error_type"] = ex.GetType().Name,
                ["assembly"] = commandType.Assembly.GetName().Name ?? "Unknown"
            });

            // Activity에 에러 상태 설정
            _telemetryService.SetActivityError(activity, ex);
            
            // 에러 로그
            _telemetryService.LogErrorWithTrace(_logger, ex, 
                "LiteBus 명령 처리 중 오류 발생: {CommandType}, 처리시간: {ElapsedMs}ms, 오류: {ErrorMessage}", 
                commandType.Name, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}
