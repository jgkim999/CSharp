using FastEndpoints;
using FluentValidation;

namespace GamePulse.DTO;

/// <summary>
/// 
/// </summary>
public class LoginRequestValidator : Validator<LoginRequest>
{
    /// <summary>
    /// 
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("name is required")
            .NotNull()
            .WithMessage("name not allowed null")
            .MinimumLength(3)
            .WithMessage("name is too short")
            .MaximumLength(8)
            .WithMessage("name is too long");
                
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("password is required")
            .NotNull()
            .WithMessage("password not allowed null")
            .MinimumLength(3)
            .WithMessage("password is too short")
            .MaximumLength(8)
            .WithMessage("password is too long");
    }
}
