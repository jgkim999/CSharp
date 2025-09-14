namespace Demo.Application.Models;

public record ProductListRequest
{
    public string? SearchTerm { get; init; }
    public long? CompanyId { get; init; }
    public int Page { get; init; } = 0;
    public int PageSize { get; init; } = 10;
}