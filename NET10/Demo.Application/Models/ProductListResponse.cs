using Demo.Application.DTO.Product;

namespace Demo.Application.Models;

public record ProductListResponse
{
    public List<ProductDto> Items { get; init; } = new();
    public int TotalItems { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}