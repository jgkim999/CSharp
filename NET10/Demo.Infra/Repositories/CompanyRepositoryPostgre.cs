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

public class CompanyRepositoryPostgre : ICompanyRepository
{
    private readonly PostgresConfig _config;
    private readonly ILogger<CompanyRepositoryPostgre> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IAsyncPolicy _retryPolicy;
    
    /// <summary>
    /// Initializes a new instance of the CompanyRepositoryPostgre class with the specified configuration, mapper, logger, and telemetry service.
    /// </summary>
    public CompanyRepositoryPostgre(
        IOptions<PostgresConfig> config,
        IMapper mapper,
        ILogger<CompanyRepositoryPostgre> logger,
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
            var whereClause = string.IsNullOrWhiteSpace(searchTerm) ? "" : "WHERE name ILIKE @searchTerm";
            var searchPattern = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm}%";

            var countQuery = $"SELECT COUNT(*) FROM companies {whereClause};";
            var dataQuery = $"SELECT id, name, created_at FROM companies {whereClause} ORDER BY id OFFSET @offset LIMIT @limit;";

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

            var companies = await _retryPolicy.ExecuteAsync(() => connection.QueryAsync<CompanyEntity>(dataQuery, dataParams));
            
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