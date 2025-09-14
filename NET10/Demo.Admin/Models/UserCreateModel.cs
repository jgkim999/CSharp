using System.ComponentModel.DataAnnotations;

namespace Demo.Admin.Models;

public class UserCreateModel
{
    [Required(ErrorMessage = "이름은 필수입니다")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "이름은 3자 이상 255자 이하여야 합니다")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "이메일은 필수입니다")]
    [EmailAddress(ErrorMessage = "유효한 이메일 주소를 입력해주세요")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "이메일은 3자 이상 255자 이하여야 합니다")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호는 필수입니다")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "비밀번호는 8자 이상 64자 이하여야 합니다")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호 확인은 필수입니다")]
    [Compare(nameof(Password), ErrorMessage = "비밀번호가 일치하지 않습니다")]
    public string ConfirmPassword { get; set; } = string.Empty;
}