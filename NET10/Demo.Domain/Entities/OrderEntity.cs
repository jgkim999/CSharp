using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Entities;

[Table("orders")]
public class OrderEntity
{
    public long id { get; set; }
    public long user_id { get; set; }
    public long product_id { get; set; }
    public int quantity { get; set; }
    public string status { get; set; }
    public decimal price { get; set; }
    public DateTime created_at { get; set; }
}
