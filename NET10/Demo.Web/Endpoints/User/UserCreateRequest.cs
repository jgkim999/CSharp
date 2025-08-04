using System.ComponentModel;
using FastEndpoints;
using FluentValidation;

namespace Demo.Web.Endpoints.User;

public class UserCreateRequest
{
    [DefaultValue("abc")]
    public string Name { get; set; } = string.Empty;
    [DefaultValue("abc@gmail.com")]
    public string Email { get; set; } = string.Empty;
    [DefaultValue("abcd1234")]
    public string Password { get; set; } = string.Empty;
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

        RuleFor(x => x.Password)
            .NotEmpty()
            .NotNull()
            .MinimumLength(8)
            .MaximumLength(64);
    }
}
