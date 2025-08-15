using FastEndpoints;
using FluentValidation;

namespace Demo.Web.DTO;

/// <summary>
/// MyRequest 유효성 검사기
/// </summary>
public class MyRequestValidator : Validator<MyRequest>
{
    /// <summary>
    /// MyRequest 객체에 대한 유효성 검사 규칙을 정의하여 MyRequestValidator 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    public MyRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("성은 필수입니다.")
            .MaximumLength(50)
            .WithMessage("성은 50자를 초과할 수 없습니다.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("이름은 필수입니다.")
            .MaximumLength(50)
            .WithMessage("이름은 50자를 초과할 수 없습니다.");

        RuleFor(x => x.Age)
            .GreaterThan(0)
            .WithMessage("나이는 0보다 커야 합니다.")
            .LessThanOrEqualTo(150)
            .WithMessage("나이는 150세를 초과할 수 없습니다.");
    }
}