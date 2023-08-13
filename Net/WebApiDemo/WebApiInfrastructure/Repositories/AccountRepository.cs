using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiInfrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ILogger<AccountRepository> _logger;
    private readonly IConfiguration _config;

    public AccountRepository(ILogger<AccountRepository> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<Account> GetAsync(string name)
    {
        try
        {
            using IDbConnection conn = new MySqlConnection(_config.GetConnectionString("Default"));
            string sql = "SELECT `Id`,`Name` FROM `Account` WHERE `Name` = @Name;";
            DynamicParameters p = new();
            p.Add("@Name", name);
            var row = await conn.QueryFirstOrDefaultAsync<Account>(sql, p);
            return row;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }

    public async Task<bool> AddAsync(string name)
    {
        try
        {
            using IDbConnection conn = new MySqlConnection(_config.GetConnectionString("Default"));
            string sql = "INSERT INTO `Account` (`Name`) VALUES (@Name);";
            DynamicParameters p = new();
            p.Add("@Name", name);
            var affectedRows = await conn.ExecuteAsync(sql, p);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }
}
