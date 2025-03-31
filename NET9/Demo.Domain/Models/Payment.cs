using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Demo.Domain.Models;

public class Payment
{
    [Key, Column(Order = 0)]
    public int CustomerNumber { get; set; }

    [Key, Column(Order = 1)]
    [MaxLength(50)]
    public required string CheckNumber { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [ForeignKey("CustomerNumber")]
    public virtual Customer? Customer { get; set; }
}
