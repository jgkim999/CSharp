using System.ComponentModel.DataAnnotations;

namespace Demo.Domain.Models;

public class ProductLineData
{
    [Key]
    [MaxLength(50)]
    public required string ProductLine { get; set; }

    [MaxLength(4000)]
    public string? TextDescription { get; set; }

    public string? HtmlDescription { get; set; }

    public byte[]? Image { get; set; }
}
