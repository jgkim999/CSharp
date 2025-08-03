using FastEndpoints;
using FluentValidation;

namespace Demo.Web.Endpoints.User;

public class UserCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordSha256 { get; set; } = string.Empty;
}

public class UserCreateRequestRequestValidator : Validator<UserCreateRequest>
{
    public UserCreateRequestRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("name is required")
            .NotNull()
            .WithMessage("name not allowed null")
            .MinimumLength(3)
            .WithMessage("name is too short")
            .MaximumLength(255)
            .WithMessage("name is too long");
                
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .NotNull()
            .WithMessage("Email not allowed null")
            .MinimumLength(3)
            .WithMessage("Email is too short")
            .MaximumLength(255)
            .WithMessage("Email is too long")
            .EmailAddress()
            .WithMessage("Not a valid email");

        RuleFor(x => x.PasswordSha256)
            .NotEmpty()
            .NotNull()
            .Length(64);
    }
}
