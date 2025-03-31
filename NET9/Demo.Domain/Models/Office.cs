using System.ComponentModel.DataAnnotations;

namespace Demo.Domain.Models;

public class Office
{
    [Key]
    [MaxLength(10)]
    public required string OfficeCode { get; set; }

    [Required]
    [MaxLength(50)]
    public required string City { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Phone { get; set; }

    [Required]
    [MaxLength(50)]
    public required string AddressLine1 { get; set; }

    [MaxLength(50)]
    public string? AddressLine2 { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Country { get; set; }

    [Required]
    [MaxLength(15)]
    public required string PostalCode { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Territory { get; set; }
}