using Dapper;
using Demo.Application.DTO.User;
using Demo.Application.Repositories;
using Demo.Application.Services;
using Demo.Infra.Configs;
using Demo.Infra.Extensions;
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
    return await _telemetryService.InstrumentDatabaseOperationAsync(
        _logger,
        "db.user.create",
        "postgresql",
        "INSERT",
        "users",
        nameof(UserRepositoryPostgre),
        nameof(CreateAsync),
        async (activity) =>
        {
          await using var connection = new NpgsqlConnection(_config.ConnectionString);

          // 연결 정보를 Activity에 추가
          activity.AddConnectionInfo(connection);

          const string sqlQuery = "INSERT INTO users (name, email, password) VALUES (@name, @email, @password)";

          // SQL 쿼리 정보를 Activity에 추가
          activity.AddSqlQueryInfo(sqlQuery, 3);

          var dp = new DynamicParameters();
          dp.Add("@name", name);
          dp.Add("@email", email);
          dp.Add("@password", passwordSha256);

          // 쿼리 실행 시간 측정
          var queryStopwatch = Stopwatch.StartNew();
          var rowsAffected = await connection.ExecuteAsync(sqlQuery, dp);
          queryStopwatch.Stop();

          var queryDuration = queryStopwatch.Elapsed.TotalMilliseconds;

          // 쿼리 결과 정보를 Activity에 추가
          activity.AddQueryResultInfo(rowsAffected, queryDuration);

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

            return Result.Fail(errorMessage);
          }
        },
        new Dictionary<string, object?>
        {
          ["db.user.email"] = email,
          ["db.user.name"] = name
        });
  }

  /// <summary>
  /// Retrieves all users from the database and returns them as a list of user DTOs.
  /// </summary>
  /// <returns>A result containing a list of user DTOs if successful; otherwise, a failure result with an error message.</returns>
  public async Task<Result<IEnumerable<UserDto>>> GetAllAsync()
  {
    return await _telemetryService.InstrumentDatabaseOperationAsync(
        _logger,
        "db.user.getall",
        "postgresql",
        "SELECT",
        "users",
        nameof(UserRepositoryPostgre),
        nameof(GetAllAsync),
        async (activity) =>
        {
          await using var connection = new NpgsqlConnection(_config.ConnectionString);

          // 연결 정보를 Activity에 추가
          activity.AddConnectionInfo(connection);

          const string sqlQuery = "SELECT id, name, email, created_at FROM users;";

          // SQL 쿼리 정보를 Activity에 추가
          activity.AddSqlQueryInfo(sqlQuery, 0);

          // 쿼리 실행 시간 측정
          var queryStopwatch = Stopwatch.StartNew();
          var users = await connection.QueryAsync<UserDb>(sqlQuery);
          queryStopwatch.Stop();

          var usersList = users.ToList();
          var userCount = usersList.Count;
          var queryDuration = queryStopwatch.Elapsed.TotalMilliseconds;

          // 조회 결과 정보를 Activity에 추가
          activity.AddSelectResultInfo(userCount, queryDuration);

          // 매핑 시간 측정
          var mappingStopwatch = Stopwatch.StartNew();
          var userDtos = _mapper.Map<List<UserDto>>(usersList);
          mappingStopwatch.Stop();

          var mappingDuration = mappingStopwatch.Elapsed.TotalMilliseconds;

          // Activity에 매핑 정보 추가
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

          return Result.Ok<IEnumerable<UserDto>>(userDtos);
        });
  }
}