using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Entities;

[Table("companies")]
public class CompanyEntity
{
    public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public DateTime created_at { get; set; }
}
