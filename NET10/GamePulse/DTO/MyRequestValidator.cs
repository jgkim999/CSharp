using FastEndpoints;
using FluentValidation;

namespace GamePulse.DTO;

public class MyRequestValidator : Validator<MyRequest>
{
    /// <summary>
    /// Initializes a new instance of the <c>MyRequestValidator</c> class, defining validation rules for <c>MyRequest</c> objects.
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
