using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Entities;

[Table("products")]
public class Product
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
