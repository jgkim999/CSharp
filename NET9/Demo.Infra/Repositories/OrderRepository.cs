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

    public async Task<int> GetOrderCount2(string state)
    {
        await using MySqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "GetOrderCountByStatus";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add(new MySqlParameter("@state", MySqlDbType.VarChar) { Value = state });
        cmd.Parameters.Add(new MySqlParameter("@total", MySqlDbType.Int32) { Direction = ParameterDirection.Output });

        await cmd.ExecuteScalarAsync();

        return (int)cmd.Parameters["@total"].Value;
    }
}
