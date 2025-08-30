using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Entities;

[Table("companies")]
public class Company
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
