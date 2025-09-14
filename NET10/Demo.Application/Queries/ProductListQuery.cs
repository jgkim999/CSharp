using Demo.Application.DTO.Product;
using Demo.Domain.Repositories;
using FluentResults;
using LiteBus.Queries.Abstractions;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Queries;

public record ProductListQuery(string? SearchTerm, long? CompanyId, int Page, int PageSize) : IQuery<Result<ProductListQueryResult>>;

public class ProductListQueryResult
{
    public List<ProductDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
}

public class ProductListQueryHandler : IQueryHandler<ProductListQuery, Result<ProductListQueryResult>>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductListQueryHandler> _logger;

    public ProductListQueryHandler(
        IProductRepository productRepository,
        ILogger<ProductListQueryHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<Result<ProductListQueryResult>> HandleAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productRepository.GetPagedWithCompanyAsync(
                query.SearchTerm,
                query.CompanyId,
                query.Page,
                query.PageSize,
                cancellationToken);

            if (result.IsFailed)
            {
                _logger.LogError("상품 목록 조회 실패: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
                return Result.Fail(result.Errors);
            }

            var productDtos = result.Value.Products.Select(p => new ProductDto
            {
                Id = p.Product.id,
                CompanyId = p.Product.company_id,
                CompanyName = p.Company.name,
                Name = p.Product.name,
                Price = p.Product.price,
                CreatedAt = p.Product.created_at
            }).ToList();

            return Result.Ok(new ProductListQueryResult
            {
                Items = productDtos,
                TotalItems = result.Value.TotalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "상품 목록 조회 중 예외 발생");
            return Result.Fail($"상품 목록 조회 실패: {ex.Message}");
        }
    }
}