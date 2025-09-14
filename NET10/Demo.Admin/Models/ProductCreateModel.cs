using System.ComponentModel.DataAnnotations;

namespace Demo.Admin.Models;

public class ProductCreateModel
{
    [Required(ErrorMessage = "회사를 선택해야 합니다")]
    public long CompanyId { get; set; }

    [Required(ErrorMessage = "상품명은 필수입니다")]
    [StringLength(100, ErrorMessage = "상품명은 100자 이하로 입력해주세요")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "가격은 필수입니다")]
    [Range(0, int.MaxValue, ErrorMessage = "가격은 0 이상이어야 합니다")]
    public int Price { get; set; }
}