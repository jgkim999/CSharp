using Demo.Application.DTO.Company;
using Demo.Domain.Repositories;
using FluentResults;
using LiteBus.Queries.Abstractions;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Queries;

public record CompanyListQuery(string? SearchTerm, int Page, int PageSize) : IQuery<Result<CompanyListQueryResult>>;

public class CompanyListQueryResult
{
    public List<CompanyDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
}

public class CompanyListQueryHandler : IQueryHandler<CompanyListQuery, Result<CompanyListQueryResult>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<CompanyListQueryHandler> _logger;

    public CompanyListQueryHandler(
        ICompanyRepository companyRepository,
        ILogger<CompanyListQueryHandler> logger)
    {
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<Result<CompanyListQueryResult>> HandleAsync(CompanyListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _companyRepository.GetPagedAsync(query.SearchTerm, query.Page, query.PageSize, cancellationToken);

            if (result.IsFailed)
            {
                _logger.LogError("회사 목록 조회 실패: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
                return Result.Fail(result.Errors);
            }

            List<CompanyDto> companyDtos = result.Value.Companies.Adapt<List<CompanyDto>>();

            return Result.Ok(new CompanyListQueryResult
            {
                Items = companyDtos,
                TotalItems = result.Value.TotalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "회사 목록 조회 중 예외 발생");
            return Result.Fail($"회사 목록 조회 실패: {ex.Message}");
        }
    }
}