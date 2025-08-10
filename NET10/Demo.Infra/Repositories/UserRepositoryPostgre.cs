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

namespace Demo.Infra.Repositories;

public class UserRepositoryPostgre : IUserRepository
{
    private readonly PostgresConfig _config;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRepositoryPostgre> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the UserRepositoryPostgre class with the specified configuration, mapper, logger, and telemetry service.
    /// </summary>
    public UserRepositoryPostgre(
    PostgresConfig config,
    IMapper mapper,
    ILogger<UserRepositoryPostgre> logger,
    ITelemetryService telemetryService)
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
      using var activity = _telemetryService.StartActivity(nameof(CreateAsync));
      try
      {
          await using var connection = new NpgsqlConnection(_config.ConnectionString);

          const string sqlQuery = "INSERT INTO users (name, email, password) VALUES (@name, @email, @password);";

          DynamicParameters dp = new();
          dp.Add("@name", name);
          dp.Add("@email", email);
          dp.Add("@password", passwordSha256);

          var rowsAffected = await connection.ExecuteAsync(sqlQuery, dp);

          if (rowsAffected == 1)
          {
              return Result.Ok();
          }

          // 삽입 실패 처리
          var errorMessage = "Insert failed - no rows affected";
          return Result.Fail(errorMessage);
      }
      catch (Exception ex)
      {
          _logger.LogError(ex, nameof(CreateAsync));
          return Result.Fail(ex.Message);
      }
    }

    /// <summary>
    /// Retrieves all users from the database and returns them as a list of user DTOs.
    /// </summary>
    /// <returns>A result containing a list of user DTOs if successful; otherwise, a failure result with an error message.</returns>
    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync(int limit = 10)
    {
        using var activity = _telemetryService.StartActivity(nameof(GetAllAsync));

        if (limit > 100)
            return Result.Fail($"Too many row request {limit}");
        try
        {
            await using var connection = new NpgsqlConnection(_config.ConnectionString);

            // 연결 정보를 Activity에 추가
            activity.AddConnectionInfo(connection);

            const string sqlQuery = "SELECT id, name, email, created_at FROM users LIMIT @count;";

            DynamicParameters dp = new();
            dp.Add("@count", limit);

            var users = await connection.QueryAsync<UserDb>(sqlQuery, dp);

            var usersList = users.ToList();
            var userDtos = _mapper.Map<List<UserDto>>(usersList);

            return Result.Ok<IEnumerable<UserDto>>(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetAllAsync));
            return Result.Fail(ex.Message);
        }
    }
}
