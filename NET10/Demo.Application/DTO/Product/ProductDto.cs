namespace Demo.Application.DTO.Product;

public record ProductDto
{
    public long Id { get; init; }
    public long CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Price { get; init; }
    public DateTime CreatedAt { get; init; }
}