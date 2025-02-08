using Dapper;

using MySqlConnector;

using System.Data;

namespace Demo.Infra.Repositories;

public class OrderRepository
{
    private string _connectionString = string.Empty;

    public OrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> GetOrderCount(string state)
    {
        MySqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        DynamicParameters parameters = new DynamicParameters();
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
