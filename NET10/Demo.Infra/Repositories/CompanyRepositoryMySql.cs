using Dapper;
using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Polly;
using SqlKata;
using SqlKata.Compilers;

namespace Demo.Infra.Repositories;

public class CompanyRepositoryMySql : ICompanyRepository
{
    private readonly MySqlConfig _mysqlConfig;
    private readonly ILogger<CompanyRepositoryMySql> _logger;
    private readonly ITelemetryService _telemetryService;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly MySqlCompiler _compiler;
    
    public CompanyRepositoryMySql(
        IOptions<MySqlConfig> config,
        ILogger<CompanyRepositoryMySql> logger,
        ITelemetryService telemetryService,
        MySqlCompiler compiler)
    {
        _mysqlConfig = config.Value;
        _logger = logger;
        _telemetryService = telemetryService;
        _compiler = compiler;
        
        _resiliencePolicy = Policy
            .Handle<MySqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retry => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retry - 1)),
                onRetry: (exception, sleep, retry, _)=>
                {
                    _logger.LogWarning(exception, "Database operation failed. Retry {Retry} in {Delay}.", retry, sleep);
                });
    }
    
    public async Task<Result> CreateAsync(string name, CancellationToken ct = default)
    {
        using var activity = _telemetryService.StartActivity("create.company");
        try
        {
            Query? query = CompanyQueryBuilder.Insert(name);
            var sqlResult = _compiler.Compile(query);
            
            var sql = sqlResult.Sql;
            var parameters = new DynamicParameters(sqlResult.NamedBindings);
            
            await using var connection = new MySqlConnection(_mysqlConfig.ConnectionString);
            await connection.OpenAsync(ct);
            
            var affectedRows = await _resiliencePolicy.ExecuteAsync(() => connection.ExecuteAsync(sql, parameters));
            if (affectedRows == 1)
            {
                return Result.Ok();
            }
            
            _logger.LogWarning("{Method} - No rows were inserted into the database.", nameof(CreateAsync));
            return Result.Fail("No rows were inserted into the database.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(CreateAsync)} failed");
            return Result.Fail(new ExceptionalError(ex));
        }
    }

    public async Task<Result<(IEnumerable<CompanyEntity> Companies, int TotalCount)>> GetPagedAsync(string? searchTerm, int page, int pageSize, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}