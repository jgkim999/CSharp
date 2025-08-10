using FastEndpoints;
using FluentValidation;

namespace GamePulse.DTO;

public class LoginRequestValidator : Validator<LoginRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestValidator"/> class, defining validation rules for <c>Username</c> and <c>Password</c> properties of a <see cref="LoginRequest"/>.
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
