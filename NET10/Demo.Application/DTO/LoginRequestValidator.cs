using FastEndpoints;
using FluentValidation;

namespace Demo.Application.DTO;

/// <summary>
/// 로그인 요청 유효성 검사기
/// </summary>
public class LoginRequestValidator : Validator<LoginRequest>
{
    /// <summary>
    /// LoginRequest의 Username과 Password 속성에 대한 유효성 검사 규칙을 정의하여 LoginRequestValidator 클래스의 새 인스턴스를 초기화합니다
    /// <summary>
    /// Initializes a new instance of <see cref="LoginRequestValidator"/> and configures validation rules for <c>LoginRequest</c>.
    /// </summary>
    /// <remarks>
    /// Validation rules:
    /// - <c>Username</c>: required (not empty, not null), length between 3 and 8 characters; messages are provided in Korean.
    /// - <c>Password</c>: required (not empty, not null), length between 3 and 8 characters; messages are provided in Korean.
    /// </remarks>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("사용자명은 필수입니다")
            .NotNull()
            .WithMessage("사용자명은 null일 수 없습니다")
            .MinimumLength(3)
            .WithMessage("사용자명은 너무 짧습니다")
            .MaximumLength(8)
            .WithMessage("사용자명은 너무 깁니다");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("비밀번호는 필수입니다")
            .NotNull()
            .WithMessage("비밀번호는 null일 수 없습니다")
            .MinimumLength(3)
            .WithMessage("비밀번호는 너무 짧습니다")
            .MaximumLength(8)
            .WithMessage("비밀번호는 너무 깁니다");
    }
}