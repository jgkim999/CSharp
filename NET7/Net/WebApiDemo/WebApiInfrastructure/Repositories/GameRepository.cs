using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;
using System.Text;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiInfrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly ILogger<GameRepository> _logger;
    private readonly IConfiguration _config;

    public GameRepository(
        ILogger<GameRepository> logger,
        IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<int> AddItemAsync(GameItem item)
    {
        try
        {
            using IDbConnection conn = new MySqlConnection(_config.GetConnectionString("Default"));
            string sql = "INSERT INTO `GameItem` (`AccountId`,`ItemId`,`Amount`) VALUES (@AccountId,@ItemId,@Amount);";
            var affectedRow = await conn.ExecuteAsync(sql, item);
            return affectedRow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }

    public async Task<int> AddAsync(List<GameItem> items)
    {
        try
        {
            using IDbConnection conn = new MySqlConnection(_config.GetConnectionString("Default"));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("INSERT INTO `GameItem` (`AccountId`,`ItemId`,`Amount`) VALUES ");
            for (var i = 0; i < (items.Count - 1); ++i)
            {
                sb.AppendLine($"({items[i].AccountId},{items[i].ItemId},{items[i].Amount}),");
            }
            sb.AppendLine($"({items[items.Count - 1].AccountId},{items[items.Count - 1].ItemId},{items[items.Count - 1].Amount});");
            var affectedRow = await conn.ExecuteAsync(sb.ToString());
            return affectedRow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }

    public async Task<IEnumerable<GameItem>> GetAllItemAsync(long userId)
    {
        try
        {
            using IDbConnection conn = new MySqlConnection(_config.GetConnectionString("Default"));
            string query = "SELECT `Id`,`AccountId`,`ItemId`,`Amount` FROM `GameItem` WHERE `AccountId` = @AccountId;";
            var items = await conn.QueryAsync<GameItem>(query, new { AccountId = userId });
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }
}
