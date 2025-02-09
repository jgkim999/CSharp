using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Models;

public class Customer
{
    [Key]
    public int CustomerNumber { get; set; }

    [Required]
    [MaxLength(50)]
    public required string CustomerName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ContactLastName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ContactFirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Phone { get; set; }

    [Required]
    [MaxLength(50)]
    public required string AddressLine1 { get; set; }

    [MaxLength(50)]
    public string? AddressLine2 { get; set; }

    [Required]
    [MaxLength(50)]
    public required string City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(15)]
    public string? PostalCode { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Country { get; set; }

    public int? SalesRepEmployeeNumber { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CreditLimit { get; set; }

    [ForeignKey("SalesRepEmployeeNumber")]
    public virtual Employee? SalesRep { get; set; }
}
