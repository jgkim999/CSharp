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

public class ProductRepositoryPostgre : IProductRepository
{
    private readonly PostgresConfig _config;
    private readonly ILogger<ProductRepositoryPostgre> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the ProductRepositoryPostgre class with the specified configuration, mapper, logger, and telemetry service.
    /// </summary>
    public ProductRepositoryPostgre(
        IOptions<PostgresConfig> config,
        IMapper mapper,
        ILogger<ProductRepositoryPostgre> logger,
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
    /// 지정된 정보로 새 상품 레코드를 데이터베이스에 비동기적으로 생성합니다.
    /// </summary>
    /// <param name="companyId">회사 ID.</param>
    /// <param name="name">상품 이름.</param>
    /// <param name="price">상품 가격.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>상품이 생성된 경우 성공을 나타내는 결과 또는 작업이 성공하지 않은 경우 오류 메시지가 포함된 실패를 반환하는 작업.</returns>
    public async Task<Result> CreateAsync(long companyId, string name, int price, CancellationToken ct = default)
    {
        using var activity = _telemetryService.StartActivity(nameof(CreateAsync));
        try
        {
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            await connection.OpenAsync(ct);

            // 먼저 회사가 존재하는지 확인
            const string checkCompanyQuery = "SELECT COUNT(*) FROM companies WHERE id = @companyId;";
            DynamicParameters checkParams = new();
            checkParams.Add("@companyId", companyId);

            var companyExists = await _retryPolicy.ExecuteAsync(() => connection.QuerySingleAsync<int>(checkCompanyQuery, checkParams));

            if (companyExists == 0)
            {
                return Result.Fail($"Company with ID {companyId} does not exist");
            }

            // 상품 생성
            const string sqlQuery = "INSERT INTO products (company_id, name, price) VALUES (@companyId, @name, @price);";

            DynamicParameters dp = new();
            dp.Add("@companyId", companyId);
            dp.Add("@name", name);
            dp.Add("@price", price);

            var rowsAffected = await _retryPolicy.ExecuteAsync(() => connection.ExecuteAsync(sqlQuery, dp));

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
    /// 상품 목록을 페이징과 선택적 검색 기능으로 비동기적으로 조회합니다.
    /// </summary>
    /// <param name="searchTerm">상품명으로 필터링할 선택적 검색어.</param>
    /// <param name="companyId">회사 ID로 필터링할 선택적 값.</param>
    /// <param name="page">페이지 번호 (0부터 시작).</param>
    /// <param name="pageSize">페이지당 항목 수.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>상품 목록과 전체 개수를 포함하는 결과를 반환하는 작업.</returns>
    public async Task<Result<(IEnumerable<ProductEntity> Products, int TotalCount)>> GetPagedAsync(string? searchTerm, long? companyId, int page, int pageSize, CancellationToken ct = default)
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
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("name ILIKE @searchTerm");
                parameters.Add("@searchTerm", $"%{searchTerm}%");
            }

            if (companyId.HasValue)
            {
                whereClauses.Add("company_id = @companyId");
                parameters.Add("@companyId", companyId.Value);
            }

            var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countQuery = $"SELECT COUNT(*) FROM products {whereClause};";
            var dataQuery = $"SELECT id, company_id, name, price, created_at FROM products {whereClause} ORDER BY id OFFSET @offset LIMIT @limit;";

            parameters.Add("@offset", page * pageSize);
            parameters.Add("@limit", pageSize);

            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            await connection.OpenAsync(ct);

            var totalCount = await _retryPolicy.ExecuteAsync(() => connection.QuerySingleAsync<int>(countQuery, parameters));

            var products = await _retryPolicy.ExecuteAsync(() => connection.QueryAsync<ProductEntity>(dataQuery, parameters));

            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Method} failed with search term: {SearchTerm}, companyId: {CompanyId}, page: {Page}, pageSize: {PageSize}",
                nameof(GetPagedAsync), searchTerm, companyId, page, pageSize);
            return Result.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 회사 정보와 함께 상품 목록을 페이징과 선택적 검색 기능으로 비동기적으로 조회합니다.
    /// </summary>
    /// <param name="searchTerm">상품명으로 필터링할 선택적 검색어.</param>
    /// <param name="companyId">회사 ID로 필터링할 선택적 값.</param>
    /// <param name="page">페이지 번호 (0부터 시작).</param>
    /// <param name="pageSize">페이지당 항목 수.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>상품과 회사 정보가 결합된 목록과 전체 개수를 포함하는 결과를 반환하는 작업.</returns>
    public async Task<Result<(IEnumerable<(ProductEntity Product, CompanyEntity Company)> Products, int TotalCount)>> GetPagedWithCompanyAsync(string? searchTerm, long? companyId, int page, int pageSize, CancellationToken ct = default)
    {
        using var activity = _telemetryService.StartActivity(nameof(GetPagedWithCompanyAsync));

        if (pageSize > 100)
            return Result.Fail($"Page size too large: {pageSize}");
        if (pageSize <= 0)
            return Result.Fail($"Page size must be positive: {pageSize}");
        if (page < 0)
            return Result.Fail($"Page must be non-negative: {page}");

        try
        {
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("p.name ILIKE @searchTerm");
                parameters.Add("@searchTerm", $"%{searchTerm}%");
            }

            if (companyId.HasValue)
            {
                whereClauses.Add("p.company_id = @companyId");
                parameters.Add("@companyId", companyId.Value);
            }

            var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countQuery = $@"
                SELECT COUNT(*)
                FROM products p
                INNER JOIN companies c ON p.company_id = c.id
                {whereClause};";

            var dataQuery = $@"
                SELECT
                    p.id, p.company_id, p.name, p.price, p.created_at,
                    c.id, c.name, c.created_at
                FROM products p
                INNER JOIN companies c ON p.company_id = c.id
                {whereClause}
                ORDER BY p.id
                OFFSET @offset LIMIT @limit;";

            parameters.Add("@offset", page * pageSize);
            parameters.Add("@limit", pageSize);

            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            await connection.OpenAsync(ct);

            var totalCount = await _retryPolicy.ExecuteAsync(() => connection.QuerySingleAsync<int>(countQuery, parameters));

            var result = await _retryPolicy.ExecuteAsync(() =>
                connection.QueryAsync<ProductEntity, CompanyEntity, (ProductEntity Product, CompanyEntity Company)>(
                    dataQuery,
                    (product, company) => (product, company),
                    parameters,
                    splitOn: "id"));

            return (result, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Method} failed with search term: {SearchTerm}, companyId: {CompanyId}, page: {Page}, pageSize: {PageSize}",
                nameof(GetPagedWithCompanyAsync), searchTerm, companyId, page, pageSize);
            return Result.Fail(ex.Message);
        }
    }
}