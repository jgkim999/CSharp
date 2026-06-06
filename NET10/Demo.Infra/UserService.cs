using Dapper;
using Demo.Application;
using Demo.Domain;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using SqlKata;

namespace Demo.Infra;

public class UserService : IUserService
{
    private readonly IDbManager _dbManager;
    private readonly ILogger<UserService> _logger;

    public UserService(IDbManager dbManager, ILogger<UserService> logger)
    {
        _dbManager = dbManager;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(string email, CancellationToken ct)
    {
        SqlResult sqlResult = MySqlUsers.SelectQuery(email);
        await using var connection = new MySqlConnection(_dbManager.GetConnectionString());
        await connection.OpenAsync(ct);
        
        var queryResult = await connection.QuerySingleOrDefaultAsync(sqlResult.RawSql, sqlResult.NamedBindings);
        return queryResult;
    }

    public async Task<User?> CreateUserAsync(string email, string password, string salt, DateTime createdAt, DateTime updatedAt, CancellationToken ct)
    {
        try
        {
            SqlResult sqlResult = MySqlUsers.InsertQuery(email, password, salt, createdAt, updatedAt);
            await using var connection = new MySqlConnection(_dbManager.GetConnectionString());
            await connection.OpenAsync(ct);

            var affectedRows = await connection.ExecuteAsync(sqlResult.RawSql, sqlResult.NamedBindings);
            if (affectedRows == 0)
                return null;

            // LAST_INSERT_ID() is connection-scoped in MySQL and returns the auto-increment value
            var id = await connection.QuerySingleAsync<long>("SELECT LAST_INSERT_ID();");
            return new User()
            {
                Id = id,
                Email = email,
                Password = password,
                Salt = salt,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, email);
            throw;
        }
    }
}
