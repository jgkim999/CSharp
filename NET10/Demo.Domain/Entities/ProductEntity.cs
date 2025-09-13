using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Entities;

[Table("products")]
public class ProductEntity
{
    public long id { get; set; }
    public long company_id { get; set; }
    public string name { get; set; } = string.Empty;
    public int price { get; set; }
    public DateTime created_at { get; set; }
}
