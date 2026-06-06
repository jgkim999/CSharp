using Demo.Application;

namespace Demo.Infra;

public class MySqlManager : IDbManager
{
    public readonly string _connectionString;

    public MySqlManager(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public string GetConnectionString()
    {
        return _connectionString;
    }
}
