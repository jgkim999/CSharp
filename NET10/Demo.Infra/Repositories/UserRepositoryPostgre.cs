using Dapper;
using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Repositories;
using Demo.Infra.Configs;

using FluentResults;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;

namespace Demo.Infra.Repositories;

public class UserRepositoryPostgre : IUserRepository
{
    private readonly PostgresConfig _config;
    private readonly ILogger<UserRepositoryPostgre> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IAsyncPolicy _retryPolicy;
    
    /// <summary>
    /// Initializes a new instance of the UserRepositoryPostgre class with the specified configuration, mapper, logger, and telemetry service.
    /// </summary>
    public UserRepositoryPostgre(
        IOptions<PostgresConfig> config,
        IMapper mapper,
        ILogger<UserRepositoryPostgre> logger,
        ITelemetryService telemetryService)
    {
        _config = config.Value;
        _logger = logger;
        _telemetryService = telemetryService;
        _retryPolicy = Policy
            .Handle<NpgsqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retry => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retry - 1)),
                onRetry: (exception, sleep, retry, _) =>
                {
                    _logger.LogWarning(exception, "Database operation failed. Retry {Retry} in {Delay}.", retry, sleep);
                });
    }


    /// <summary>
    /// Asynchronously creates a new user record in the database with the specified name, email, and password hash.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordSha256">The SHA-256 hash of the user's password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result{UserEntity}"/> containing the created user entity if successful, or failure with an error message if the operation did not succeed.</returns>
    public async Task<Result<UserEntity>> CreateAsync(string name, string email, string passwordSha256, CancellationToken ct = default)
    {
      using var activity = _telemetryService.StartActivity("db.user.create");
      try
      {
          await using var connection = new NpgsqlConnection(_config.ConnectionString);
          await connection.OpenAsync(ct);

          const string sqlQuery = "INSERT INTO users (name, email, password) VALUES (@name, @email, @password) RETURNING id, name, email, created_at;";

          DynamicParameters dp = new();
          dp.Add("@name", name);
          dp.Add("@email", email);
          dp.Add("@password", passwordSha256);

          var createdUser = await _retryPolicy.ExecuteAsync(() => connection.QueryFirstOrDefaultAsync<UserEntity>(sqlQuery, dp));

          if (createdUser != null)
          {
              return Result.Ok(createdUser);
          }

          // 삽입 실패 처리
          var errorMessage = "Insert failed - no user returned";
          return Result.Fail(errorMessage);
      }
      catch (Exception ex)
      {
          _logger.LogError(ex, $"{nameof(CreateAsync)} failed");
          return Result.Fail(new ExceptionalError(ex));
      }
    }
    
    /// <summary>
    /// Asynchronously retrieves users with pagination and optional search functionality.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter users by name.</param>
    /// <param name="page">Page number (0-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to a result containing a tuple of users and total count.</returns>
    public async Task<Result<(IEnumerable<UserEntity> Users, int TotalCount)>> GetPagedAsync(string? searchTerm, int page, int pageSize, CancellationToken ct = default)
    {
        using var activity = _telemetryService.StartActivity(nameof(GetPagedAsync));

        if (pageSize > 100)
            return Result.Fail($"Page size too large: {pageSize}");
        if (pageSize <= 0)
            return Result.Fail($"Page size must be positive: {pageSize}");
        if (page < 0)
            return Result.Fail($"Page must be non-negative: {page}");

        try
        {
            // 검색 조건 구성
            var whereClause = string.IsNullOrWhiteSpace(searchTerm) ? "" : "WHERE name ILIKE @searchTerm";
            var searchPattern = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm}%";

            // 총 개수 조회
            var countQuery = $"SELECT COUNT(*) FROM users {whereClause};";
            var dataQuery = $"SELECT id, name, email, created_at FROM users {whereClause} ORDER BY id OFFSET @offset LIMIT @limit;";

            DynamicParameters countParams = new();
            DynamicParameters dataParams = new();
            
            if (!string.IsNullOrWhiteSpace(searchPattern))
            {
                countParams.Add("@searchTerm", searchPattern);
                dataParams.Add("@searchTerm", searchPattern);
            }
            
            dataParams.Add("@offset", page * pageSize);
            dataParams.Add("@limit", pageSize);

            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            await connection.OpenAsync(ct);
            
            var totalCount = await _retryPolicy.ExecuteAsync(() => connection.QuerySingleAsync<int>(countQuery, countParams));

            var users = await _retryPolicy.ExecuteAsync(() => connection.QueryAsync<UserEntity>(dataQuery, dataParams));
            
            return (users, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Method} failed with search term: {SearchTerm}, page: {Page}, pageSize: {PageSize}", 
                nameof(GetPagedAsync), searchTerm, page, pageSize);
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result<UserEntity?>> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            var dataQuery = $"SELECT id, name, email, created_at FROM users WHERE id = @id;";
            
            DynamicParameters dataParams = new();
            dataParams.Add("@id", id);
            
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            var user = await _retryPolicy.ExecuteAsync(() => connection.QueryFirstOrDefaultAsync<UserEntity>(dataQuery, dataParams));
            return user;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Method} failed with id: {Id}", nameof(FindByIdAsync), id);
            return Result.Fail(e.Message);
        }
    }
}
