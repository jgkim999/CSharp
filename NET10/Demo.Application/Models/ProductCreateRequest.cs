namespace Demo.Application.Models;

public record ProductCreateRequest
{
    public long CompanyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Price { get; init; }
}