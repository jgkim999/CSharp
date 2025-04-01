using Microsoft.Extensions.Logging;

using SQLite;

using Unity.Tools.Models;

namespace Unity.Tools.Repositories;

public class SqliteDependencyDb : IDependencyDb
{
    private readonly ILogger _logger;
    private readonly SQLiteConnection _db;

    public SqliteDependencyDb(string dbname, ILogger logger)
    {
        _logger = logger;

        var databasePath = Path.Combine("f:\\temp\\", dbname);
        _db = new SQLiteConnection(databasePath);
        _db.CreateTable<AssetDirectory>();
        _db.CreateTable<AssetFile>();
        _db.CreateTable<AssetDependency>();
    }

    public void AddDirectory(Dictionary<int, string> directoryMap, IProgressContext progressContext)
    {
        int commitCount = 0;
        _db.BeginTransaction();
        foreach (var directory in directoryMap)
        {
            ++commitCount;
            progressContext.Increment(1);
            try
            {
                var assetDirectory = new AssetDirectory() { Id = directory.Key, Name = directory.Value };
                _db.Insert(assetDirectory);

                // 트랜잭션이 너무 커지지 않도록 주기적으로 커밋
                if (commitCount % 300 == 0)
                {
                    _db.Commit();
                    _db.BeginTransaction();
                }
            }
            catch (Exception e)
            {
                _db.Rollback();
                _logger.LogError(e, "Failed to add directories in bulk");
            }
        }

        // 마지막 남은 데이터 커밋
        _db.Commit();
    }
}
