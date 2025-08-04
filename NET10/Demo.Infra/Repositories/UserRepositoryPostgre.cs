using Dapper;
using Demo.Application.DTO.User;
using Demo.Application.Repositories;
using Demo.Infra.Configs;
using FluentResults;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Demo.Infra.Repositories;

public class UserRepositoryPostgre : IUserRepository
{
    private readonly PostgresConfig _config;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRepositoryPostgre> _logger;
    
    /// <summary>
    /// Initializes a new instance of the UserRepositoryPostgre class with the specified configuration, mapper, and logger.
    /// </summary>
    public UserRepositoryPostgre(PostgresConfig config, IMapper mapper, ILogger<UserRepositoryPostgre> logger)
    {
        _config = config;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Asynchronously creates a new user record in the PostgreSQL database with the specified name, email, and password hash.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordSha256">The SHA-256 hash of the user's password.</param>
    /// <returns>A <see cref="Result"/> indicating success if the user was created, or failure with an error message if the operation did not succeed.</returns>
    public async Task<Result> CreateAsync(string name, string email, string passwordSha256)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            var user = new UserDb
            {
                name = name,
                email = email,
                password = passwordSha256
            };
            const string sqlQuery = "INSERT INTO users (name, email, password) VALUES (@name, @email, @password)";
            var dp = new DynamicParameters();
            dp.Add("@name", name);
            dp.Add("@email", email);
            dp.Add("@password", passwordSha256);
            var rowsAffected = await connection.ExecuteAsync(sqlQuery, dp);
            return rowsAffected == 1 ? Result.Ok() : Result.Fail("Insert failed");
        }
        catch (Exception e)
        {
            _logger.LogError(e, nameof(CreateAsync));
            return Result.Fail(e.ToString());
        }
    }

    /// <summary>
    /// Retrieves all users from the database and returns them as a list of user DTOs.
    /// </summary>
    /// <returns>A result containing a list of user DTOs if successful; otherwise, a failure result with an error message.</returns>
    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            // users가 null인지 확인
            var users = await connection.QueryAsync<UserDb>("SELECT id, name, email, created_at FROM users;");

            // List<UserDto> userDtos = new();
            // foreach (var user in users)
            // {
            //     var userDto = user.Adapt<UserDto>();
            //     userDtos.Add(userDto);
            // }

            var userDtos = _mapper.Map<List<UserDto>>(users);
            if (userDtos == null)
            {
                _logger.LogError("Mapping configuration error - could not map UserDb to UserDto");
                return Result.Fail("Mapping configuration error");
            }
            
            return userDtos;
        }
        catch (Exception e)
        {
            _logger.LogError(e, nameof(GetAllAsync));
            return Result.Fail(nameof(GetAllAsync));
        }
    }
}
