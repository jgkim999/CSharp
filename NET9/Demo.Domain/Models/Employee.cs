using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Demo.Domain.Models;

public class Employee
{
    [Key]
    public int EmployeeNumber { get; set; }

    [Required]
    [MaxLength(50)]
    public required string LastName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Extension { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Email { get; set; }

    [Required]
    [MaxLength(10)]
    public required string OfficeCode { get; set; }

    public int? ReportsTo { get; set; }

    [Required]
    [MaxLength(50)]
    public required string JobTitle { get; set; }

    [ForeignKey("ReportsTo")]
    public virtual Employee? Manager { get; set; }

    [ForeignKey("OfficeCode")]
    public virtual Office? Office { get; set; }
}
