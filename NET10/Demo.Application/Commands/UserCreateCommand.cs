using System.ComponentModel.DataAnnotations;
using Demo.Domain.Entities;
using Demo.Domain.Repositories;
using FluentResults;
using LiteBus.Commands.Abstractions;

namespace Demo.Application.Commands;

public record UserCreateCommandResult(Result<UserEntity> Result);

public record UserCreateCommand(string Name, string Email, string Password) : ICommand<UserCreateCommandResult>;

public class UserCreateCommandValidator : ICommandValidator<UserCreateCommand>
{
    /// <summary>
    /// Validates the <see cref="UserCreateCommand"/>, ensuring the Name property is not null, empty, or whitespace.
    /// </summary>
    /// <param name="command">The user creation command to validate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <exception cref="ValidationException">Thrown if the Name property is invalid.</exception>
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
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreateCommandHandler"/> class with the specified user repository.
    /// </summary>
    public UserCreateCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }
    
    /// <summary>
    /// Handles the user creation command by hashing the password and creating a new user in the repository.
    /// </summary>
    /// <param name="command">The command containing user details to create.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A result object indicating the outcome of the user creation operation.</returns>
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
