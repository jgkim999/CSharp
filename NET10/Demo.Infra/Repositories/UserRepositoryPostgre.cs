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
    
    public UserRepositoryPostgre(PostgresConfig config, IMapper mapper, ILogger<UserRepositoryPostgre> logger)
    {
        _config = config;
        _mapper = mapper;
        _logger = logger;
    }

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
