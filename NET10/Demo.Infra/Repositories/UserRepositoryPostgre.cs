using Dapper;
using Demo.Application.DTO;
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

    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_config.ConnectionString);
            var users = await connection.QueryAsync<UserDb>("SELECT * FROM users;");
            var userDtos = _mapper.Map<List<UserDto>>(users);
            return userDtos;
        }
        catch (Exception e)
        {
            _logger.LogError(e, nameof(GetAllAsync));
            return Result.Fail(nameof(GetAllAsync));
        }
    }
}
