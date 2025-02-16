using Dapper;

using Demo.Application.Repositories;
using Demo.Infra.Config;
using FluentResults;

using Microsoft.Extensions.Options;

using MySqlConnector;

using System.Data;

namespace Demo.Infra.Repositories;

public class OrderRepository : IOrderRepository
{
    private MySqlConfig _mySqlConfig;

    public OrderRepository(IOptions<MySqlConfig> mySqlConfig)
    {
        _mySqlConfig = mySqlConfig.Value;
    }

    public async Task<Result<int>> GetOrderCount(string state)
    {
        await using MySqlConnection conn = new(_mySqlConfig.ConnectionString);
        await conn.OpenAsync();

        DynamicParameters parameters = new();
        parameters.Add("orderStatus", state);
        parameters.Add("@total", dbType: DbType.Int32, direction: ParameterDirection.Output);

        //Execute stored procedure and map the returned result to a Customer object  
        var total = await conn.ExecuteAsync(
            "GetOrderCountByStatus",
            parameters,
            commandType: CommandType.StoredProcedure);

        // Get the output parameter value
        var outputParamValue = parameters.Get<int>("@total");

        return outputParamValue;
    }
}
