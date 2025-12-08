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
using SqlKata;
using SqlKata.Compilers;

namespace Demo.Infra.Repositories;

public class CompanyRepositoryPostgre : ICompanyRepository
{
    private readonly PostgresConfig _config;
    private readonly ILogger<CompanyRepositoryPostgre> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly PostgresCompiler _compiler;
    
    /// <summary>
    /// Initializes a new instance of the CompanyRepositoryPostgre class with the specified configuration, mapper, logger, and telemetry service.
    /// </summary>
    public CompanyRepositoryPostgre(
        IOptions<PostgresConfig> config,
        IMapper mapper,
        ILogger<CompanyRepositoryPostgre> logger,
        ITelemetryService telemetryService,
        PostgresCompiler compiler)
    {
        _config = config.Value;
        _logger = logger;
        _telemetryService = telemetryService;
        _compiler = compiler;
        
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
    /// 지정된 이름으로 새 회사 레코드를 데이터베이스에 비동기적으로 생성합니다.
    /// </summary>
    /// <param name="name">회사 이름.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>회사가 생성된 경우 성공을 나타내는 결과 또는 작업이 성공하지 않은 경우 오류 메시지가 포함된 실패를 반환하는 작업.</returns>
    public async Task<Result> CreateAsync(string name, CancellationToken ct = default)
    {
        using var activity = _telemetryService.StartActivity(nameof(CreateAsync));
        try
        {
            Query? query = CompanyQueryBuilder.Insert(name);
            var sqlResult = _compiler.Compile(query);
            
            var sql = sqlResult.Sql;
            var parameters = new DynamicParameters(sqlResult.NamedBindings);
            
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            await connection.OpenAsync(ct);
            
            var rowsAffected = await _retryPolicy.ExecuteAsync(() => connection.ExecuteAsync(sql, parameters));
            if (rowsAffected == 1)
            {
                return Result.Ok();
            }

            var errorMessage = "Insert failed - no rows affected";
            return Result.Fail(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(CreateAsync)} failed");
            return Result.Fail(new ExceptionalError(ex));
        }
    }

    /// <summary>
    /// 회사 목록을 페이징과 선택적 검색 기능으로 비동기적으로 조회합니다.
    /// </summary>
    /// <param name="searchTerm">회사명으로 필터링할 선택적 검색어.</param>
    /// <param name="page">페이지 번호 (0부터 시작).</param>
    /// <param name="pageSize">페이지당 항목 수.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>회사 목록과 전체 개수를 포함하는 결과를 반환하는 작업.</returns>
    public async Task<Result<(IEnumerable<CompanyEntity> Companies, int TotalCount)>> GetPagedAsync(string? searchTerm, int page, int pageSize, CancellationToken ct = default)
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
            var countQuery = CompanyQueryBuilder.Count(searchTerm);
            var countQueryResult = _compiler.Compile(countQuery);
            DynamicParameters countParams = new(countQueryResult.Bindings);
            
            var dataQuery = CompanyQueryBuilder.Select(searchTerm, page, pageSize);
            var dataQueryResult = _compiler.Compile(dataQuery);
            DynamicParameters dataParams = new(dataQueryResult.Bindings);
            
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            await connection.OpenAsync(ct);
            
            var totalCount = await _retryPolicy.ExecuteAsync(() => connection.QuerySingleAsync<int>(countQueryResult.Sql, countParams));
            var companies = await _retryPolicy.ExecuteAsync(() => connection.QueryAsync<CompanyEntity>(dataQueryResult.Sql, dataParams));
            
            return (companies, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Method} failed with search term: {SearchTerm}, page: {Page}, pageSize: {PageSize}", 
                nameof(GetPagedAsync), searchTerm, page, pageSize);
            return Result.Fail(ex.Message);
        }
    }
}
