using System.ComponentModel.DataAnnotations;

namespace Demo.Admin.Models;

public class CompanyCreateModel
{
    [Required(ErrorMessage = "회사명은 필수입니다")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "회사명은 2자 이상 255자 이하여야 합니다")]
    public string Name { get; set; } = string.Empty;
}