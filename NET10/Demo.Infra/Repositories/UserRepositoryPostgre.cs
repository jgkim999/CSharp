using Dapper;
using Demo.Application.DTO.User;
using Demo.Application.Repositories;
using Demo.Application.Services;
using Demo.Infra.Configs;
using FluentResults;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;

namespace Demo.Infra.Repositories;

public class UserRepositoryPostgre : IUserRepository
{
    private readonly PostgresConfig _config;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRepositoryPostgre> _logger;
    private readonly TelemetryService _telemetryService;
    
    /// <summary>
    /// Initializes a new instance of the UserRepositoryPostgre class with the specified configuration, mapper, logger, and telemetry service.
    /// </summary>
    public UserRepositoryPostgre(
        PostgresConfig config, 
        IMapper mapper, 
        ILogger<UserRepositoryPostgre> logger,
        TelemetryService telemetryService)
    {
        _config = config;
        _mapper = mapper;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Asynchronously creates a new user record in the PostgreSQL database with the specified name, email, and password hash.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordSha256">The SHA-256 hash of the user's password.</param>
    /// <returns>A <see cref="Result"/> indicating success if the user was created, or failure with an error message if the operation did not succeed.</returns>
    public async Task<Result> CreateAsync(string name, string email, string passwordSha256)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // 사용자 정의 Activity 시작
        using var activity = _telemetryService.StartActivity("db.user.create", new Dictionary<string, object?>
        {
            ["db.system"] = "postgresql",
            ["db.operation"] = "INSERT",
            ["db.table"] = "users",
            ["db.user.email"] = email,
            ["db.user.name"] = name,
            ["repository.class"] = nameof(UserRepositoryPostgre),
            ["repository.method"] = nameof(CreateAsync)
        });

        try
        {
            // 데이터베이스 작업 시작 로그
            TelemetryService.LogInformationWithTrace(_logger, 
                "사용자 생성 데이터베이스 작업 시작 - Email: {Email}, Name: {Name}", email, name);

            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            
            // 연결 열기 전에 Activity 태그 추가
            activity?.SetTag("db.connection_string.host", connection.Host);
            activity?.SetTag("db.connection_string.database", connection.Database);
            activity?.SetTag("db.connection_string.port", connection.Port);

            var user = new UserDb
            {
                name = name,
                email = email,
                password = passwordSha256
            };
            
            const string sqlQuery = "INSERT INTO users (name, email, password) VALUES (@name, @email, @password)";
            
            // SQL 쿼리를 Activity에 추가 (민감한 데이터 제외)
            activity?.SetTag("db.statement", "INSERT INTO users (name, email, password) VALUES (?, ?, ?)");
            
            var dp = new DynamicParameters();
            dp.Add("@name", name);
            dp.Add("@email", email);
            dp.Add("@password", passwordSha256);
            
            // 쿼리 실행 시간 측정
            var queryStopwatch = Stopwatch.StartNew();
            var rowsAffected = await connection.ExecuteAsync(sqlQuery, dp);
            queryStopwatch.Stop();
            
            stopwatch.Stop();
            var totalDuration = stopwatch.Elapsed.TotalMilliseconds;
            var queryDuration = queryStopwatch.Elapsed.TotalMilliseconds;

            // Activity에 성능 정보 추가
            activity?.SetTag("db.rows_affected", rowsAffected);
            activity?.SetTag("db.query_duration_ms", queryDuration);
            activity?.SetTag("db.total_duration_ms", totalDuration);

            if (rowsAffected == 1)
            {
                // 성공 처리
                TelemetryService.SetActivitySuccess(activity, "사용자가 성공적으로 데이터베이스에 생성되었습니다");
                
                // 성공 메트릭 기록
                _telemetryService.RecordBusinessMetric("db_user_create_success", 1, new Dictionary<string, object?>
                {
                    ["table"] = "users",
                    ["operation"] = "insert"
                });

                // 성공 로그
                TelemetryService.LogInformationWithTrace(_logger, 
                    "사용자 데이터베이스 생성 성공 - Email: {Email}, Duration: {Duration}ms", 
                    email, totalDuration);

                return Result.Ok();
            }
            else
            {
                // 삽입 실패 처리
                var errorMessage = "Insert failed - no rows affected";
                
                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                activity?.SetTag("error", true);
                activity?.SetTag("error.type", "InsertFailed");
                activity?.SetTag("error.message", errorMessage);

                // 실패 메트릭 기록
                _telemetryService.RecordError("InsertFailed", "db_user_create", errorMessage);
                _telemetryService.RecordBusinessMetric("db_user_create_failures", 1, new Dictionary<string, object?>
                {
                    ["table"] = "users",
                    ["operation"] = "insert",
                    ["error_type"] = "no_rows_affected"
                });

                // 실패 로그
                TelemetryService.LogWarningWithTrace(_logger, 
                    "사용자 데이터베이스 생성 실패 - Email: {Email}, RowsAffected: {RowsAffected}", 
                    email, rowsAffected);

                return Result.Fail(errorMessage);
            }
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalMilliseconds;

            // Activity에 예외 정보 설정
            TelemetryService.SetActivityError(activity, e);
            activity?.SetTag("db.total_duration_ms", duration);

            // 예외 메트릭 기록
            _telemetryService.RecordError("DatabaseException", "db_user_create", e.Message);
            _telemetryService.RecordBusinessMetric("db_user_create_exceptions", 1, new Dictionary<string, object?>
            {
                ["table"] = "users",
                ["operation"] = "insert",
                ["exception_type"] = e.GetType().Name
            });

            // 예외 로그
            TelemetryService.LogErrorWithTrace(_logger, e, 
                "사용자 데이터베이스 생성 중 예외 발생 - Email: {Email}", email);

            return Result.Fail(e.ToString());
        }
    }

    /// <summary>
    /// Retrieves all users from the database and returns them as a list of user DTOs.
    /// </summary>
    /// <returns>A result containing a list of user DTOs if successful; otherwise, a failure result with an error message.</returns>
    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // 사용자 정의 Activity 시작
        using var activity = _telemetryService.StartActivity("db.user.getall", new Dictionary<string, object?>
        {
            ["db.system"] = "postgresql",
            ["db.operation"] = "SELECT",
            ["db.table"] = "users",
            ["repository.class"] = nameof(UserRepositoryPostgre),
            ["repository.method"] = nameof(GetAllAsync)
        });

        try
        {
            // 데이터베이스 작업 시작 로그
            TelemetryService.LogInformationWithTrace(_logger, "모든 사용자 조회 데이터베이스 작업 시작");

            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            
            // 연결 정보를 Activity에 추가
            activity?.SetTag("db.connection_string.host", connection.Host);
            activity?.SetTag("db.connection_string.database", connection.Database);
            activity?.SetTag("db.connection_string.port", connection.Port);

            const string sqlQuery = "SELECT id, name, email, created_at FROM users;";
            activity?.SetTag("db.statement", sqlQuery);

            // 쿼리 실행 시간 측정
            var queryStopwatch = Stopwatch.StartNew();
            var users = await connection.QueryAsync<UserDb>(sqlQuery);
            queryStopwatch.Stop();

            var usersList = users.ToList();
            var userCount = usersList.Count;
            var queryDuration = queryStopwatch.Elapsed.TotalMilliseconds;

            // Activity에 쿼리 결과 정보 추가
            activity?.SetTag("db.rows_returned", userCount);
            activity?.SetTag("db.query_duration_ms", queryDuration);

            // 매핑 시간 측정
            var mappingStopwatch = Stopwatch.StartNew();
            var userDtos = _mapper.Map<List<UserDto>>(usersList);
            mappingStopwatch.Stop();

            stopwatch.Stop();
            var totalDuration = stopwatch.Elapsed.TotalMilliseconds;
            var mappingDuration = mappingStopwatch.Elapsed.TotalMilliseconds;

            // Activity에 성능 정보 추가
            activity?.SetTag("db.total_duration_ms", totalDuration);
            activity?.SetTag("mapping.duration_ms", mappingDuration);

            if (userDtos == null)
            {
                // 매핑 실패 처리
                var errorMessage = "Mapping configuration error - could not map UserDb to UserDto";
                
                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                activity?.SetTag("error", true);
                activity?.SetTag("error.type", "MappingError");
                activity?.SetTag("error.message", errorMessage);

                // 매핑 에러 메트릭 기록
                _telemetryService.RecordError("MappingError", "db_user_getall", errorMessage);
                _telemetryService.RecordBusinessMetric("db_user_getall_mapping_errors", 1, new Dictionary<string, object?>
                {
                    ["table"] = "users",
                    ["operation"] = "select",
                    ["user_count"] = userCount
                });

                // 매핑 에러 로그
                TelemetryService.LogErrorWithTrace(_logger, 
                    new InvalidOperationException(errorMessage),
                    "사용자 데이터 매핑 실패 - UserCount: {UserCount}", userCount);

                return Result.Fail(errorMessage);
            }

            // 성공 처리
            TelemetryService.SetActivitySuccess(activity, $"{userCount}명의 사용자를 성공적으로 조회했습니다");

            // 성공 메트릭 기록
            _telemetryService.RecordBusinessMetric("db_user_getall_success", 1, new Dictionary<string, object?>
            {
                ["table"] = "users",
                ["operation"] = "select",
                ["user_count"] = userCount
            });

            // 성공 로그
            TelemetryService.LogInformationWithTrace(_logger, 
                "모든 사용자 조회 성공 - UserCount: {UserCount}, QueryDuration: {QueryDuration}ms, MappingDuration: {MappingDuration}ms, TotalDuration: {TotalDuration}ms", 
                userCount, queryDuration, mappingDuration, totalDuration);

            return userDtos;
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalMilliseconds;

            // Activity에 예외 정보 설정
            TelemetryService.SetActivityError(activity, e);
            activity?.SetTag("db.total_duration_ms", duration);

            // 예외 메트릭 기록
            _telemetryService.RecordError("DatabaseException", "db_user_getall", e.Message);
            _telemetryService.RecordBusinessMetric("db_user_getall_exceptions", 1, new Dictionary<string, object?>
            {
                ["table"] = "users",
                ["operation"] = "select",
                ["exception_type"] = e.GetType().Name
            });

            // 예외 로그
            TelemetryService.LogErrorWithTrace(_logger, e, "모든 사용자 조회 중 예외 발생");

            return Result.Fail(nameof(GetAllAsync));
        }
    }
}
