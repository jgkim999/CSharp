using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Demo.Domain.Models;

public class Order
{
    [Key]
    public int OrderNumber { get; set; }

    [Required]
    public DateTime OrderDate { get; set; }

    [Required]
    public DateTime RequiredDate { get; set; }

    public DateTime? ShippedDate { get; set; }

    [Required]
    [MaxLength(15)]
    public required string Status { get; set; }

    public string? Comments { get; set; }

    [Required]
    public int CustomerNumber { get; set; }

    [ForeignKey("CustomerNumber")]
    public virtual Customer? Customer { get; set; }

}
