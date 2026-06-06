using Demo.Domain;
using FastEndpoints;
using FluentValidation;

namespace WebApiService.Endpoints.Account.Login;

public class LoginValidator : Validator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .NotEmpty()
            .WithMessage("your email is required!")
            .MinimumLength(5)
            .WithMessage("your email is too short!");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("we need your age!")
            .Length(64)
            .WithMessage("you are not legal yet!");
    }
}
