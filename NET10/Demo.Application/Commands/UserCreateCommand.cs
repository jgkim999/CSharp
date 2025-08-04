using System.ComponentModel.DataAnnotations;
using Demo.Application.Repositories;
using FluentResults;
using LiteBus.Commands.Abstractions;

namespace Demo.Application.Commands;

public record UserCreateCommandResult(Result Result);

public record UserCreateCommand(string Name, string Email, string Password) : ICommand<UserCreateCommandResult>;

public class UserCreateCommandValidator : ICommandValidator<UserCreateCommand>
{
    public Task ValidateAsync(UserCreateCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("Product title cannot be empty");
        return Task.CompletedTask;
    }
}

public class UserCreateCommandHandler : ICommandHandler<UserCreateCommand, UserCreateCommandResult>
{
    private readonly IUserRepository _repository;
    
    public UserCreateCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<UserCreateCommandResult> HandleAsync(UserCreateCommand command, CancellationToken cancellationToken = default)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(command.Password);
        var hash = sha256.ComputeHash(bytes);
        string passwordSha256 = Convert.ToBase64String(hash);
        var result = await _repository.CreateAsync(command.Name, command.Email, passwordSha256);
        return new UserCreateCommandResult(result);
    }
}
