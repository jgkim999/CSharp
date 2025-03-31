using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Demo.Domain.Models;

public class Product
{
    [Key]
    [MaxLength(15)]
    public required string ProductCode { get; set; }

    [Required]
    [MaxLength(70)]
    public required string ProductName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ProductLine { get; set; }

    [Required]
    [MaxLength(10)]
    public required string ProductScale { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ProductVendor { get; set; }

    [Required]
    public required string ProductDescription { get; set; }

    [Required]
    public short QuantityInStock { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal BuyPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal MSRP { get; set; }

    [ForeignKey("ProductLine")]
    public virtual ProductLineData? ProductLineData { get; set; }
}
