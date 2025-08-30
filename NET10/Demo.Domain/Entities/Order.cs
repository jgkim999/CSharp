using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Entities;

[Table("orders")]
public class Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}